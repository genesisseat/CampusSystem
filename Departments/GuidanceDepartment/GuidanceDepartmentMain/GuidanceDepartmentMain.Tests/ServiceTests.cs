using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using GuidanceDepartmentMain.Contracts;
using GuidanceDepartmentMain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GuidanceDepartmentMain.Tests;

public sealed class ServiceTests
{
    private static StudentRequestDto Input => new("Housing", "Need help", "<script>alert(1)</script>", RequestUrgency.Normal);

    [Fact]
    public async Task Create_rejects_request_owned_by_another_student()
    {
        var store = new InMemoryGuidanceRequestStore();
        var service = new StudentRequestService(store, new StudentRequestValidator());
        var created = await service.CreateAsync(Guid.NewGuid(), Input, "one", CancellationToken.None);
        var result = await service.GetAsync(Guid.NewGuid(), created.Value!.Id, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal("not_found", result.ErrorCode);
    }

    [Fact]
    public async Task Create_replays_duplicate_idempotency_key()
    {
        var store = new InMemoryGuidanceRequestStore(); var service = new StudentRequestService(store, new StudentRequestValidator()); var student = Guid.NewGuid();
        var first = await service.CreateAsync(student, Input, "same", CancellationToken.None); var second = await service.CreateAsync(student, Input with { Subject = "Different" }, "same", CancellationToken.None);
        Assert.Equal(first.Value!.Id, second.Value!.Id); Assert.Equal("&lt;script&gt;alert(1)&lt;/script&gt;", first.Value.SafetyValveText);
    }

    [Fact]
    public async Task Triage_returns_typed_concurrency_conflict()
    {
        var store = new Mock<IGuidanceRequestStore>(); var audit = new Mock<IAuditLogService>(); var request = new GuidanceRequestRecord { Id = Guid.NewGuid(), Status = RequestStatus.Requested };
        store.Setup(x => x.FindAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request); store.Setup(x => x.SaveAsync(request, It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException());
        var result = await new CounselorTriageService(store.Object, audit.Object).TransitionAsync("counselor", new(request.Id, RequestStatus.InProgress, [1]), CancellationToken.None);
        Assert.Equal("conflict", result.ErrorCode); audit.Verify(x => x.AppendAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Csv_validation_reports_schema_failure_without_commit()
    {
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Wrong,Header\n1,Name\n")); var result = await new CsvImportService().ValidateAsync(stream, CancellationToken.None);
        Assert.False(result.Passed); Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Notification_retries_and_fails_gracefully()
    {
        var transport = new Mock<IOutboundMessageTransport>(); transport.Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
        var audit = new Mock<IAuditLogService>(); var service = new NotificationService(transport.Object, audit.Object, NullLogger<NotificationService>.Instance);
        Assert.False(await service.SendAsync(new("student@example.edu", "Subject", "Body"), CancellationToken.None));
        transport.Verify(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.AtLeast(2)); audit.Verify(x => x.AppendAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}