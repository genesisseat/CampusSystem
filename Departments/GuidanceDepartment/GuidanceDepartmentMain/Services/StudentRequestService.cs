using System.Net;
using FluentValidation;
using GuidanceDepartmentMain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GuidanceDepartmentMain.Services;

public sealed class StudentRequestService(IGuidanceRequestStore store, IValidator<StudentRequestDto> validator)
    : IStudentRequestService
{
    public async Task<ServiceResult<StudentRequestResponse>> CreateAsync(Guid studentId, StudentRequestDto request, string idempotencyKey, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ServiceResult<StudentRequestResponse>.Fail("validation", string.Join("; ", validation.Errors.Select(x => x.ErrorMessage)));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return ServiceResult<StudentRequestResponse>.Fail("idempotency_required", "Idempotency-Key is required.");
        var replay = await store.FindByIdempotencyKeyAsync(studentId, idempotencyKey, cancellationToken);
        if (replay is not null) return ServiceResult<StudentRequestResponse>.Ok(ToResponse(replay));
        var record = new GuidanceRequestRecord { Id = Guid.NewGuid(), StudentId = studentId, Subject = request.Subject.Trim(), Details = request.Details.Trim(), SafetyValveText = Sanitize(request.SafetyValveText), Urgency = request.Urgency, Status = RequestStatus.Requested, IdempotencyKey = idempotencyKey };
        await store.AddAsync(record, cancellationToken);
        return ServiceResult<StudentRequestResponse>.Ok(ToResponse(record));
    }

    public async Task<ServiceResult<StudentRequestResponse>> GetAsync(Guid studentId, Guid requestId, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(requestId, cancellationToken);
        return request is null || request.StudentId != studentId ? ServiceResult<StudentRequestResponse>.Fail("not_found", "Request not found.") : ServiceResult<StudentRequestResponse>.Ok(ToResponse(request));
    }

    public async Task<ServiceResult<StudentRequestResponse>> UpdateAsync(Guid studentId, Guid requestId, StudentRequestDto input, byte[] rowVersion, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(requestId, cancellationToken);
        if (request is null || request.StudentId != studentId) return ServiceResult<StudentRequestResponse>.Fail("not_found", "Request not found.");
        var validation = await validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid) return ServiceResult<StudentRequestResponse>.Fail("validation", string.Join("; ", validation.Errors.Select(x => x.ErrorMessage)));
        request.Subject = input.Subject.Trim(); request.Details = input.Details.Trim(); request.SafetyValveText = Sanitize(input.SafetyValveText); request.Urgency = input.Urgency;
        try { await store.SaveAsync(request, rowVersion, cancellationToken); return ServiceResult<StudentRequestResponse>.Ok(ToResponse(request)); }
        catch (DbUpdateConcurrencyException) { return ServiceResult<StudentRequestResponse>.Fail("conflict", "Request changed; please refresh."); }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid studentId, Guid requestId, byte[] rowVersion, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(requestId, cancellationToken);
        if (request is null || request.StudentId != studentId) return ServiceResult<bool>.Fail("not_found", "Request not found.");
        try { return ServiceResult<bool>.Ok(await store.DeleteAsync(requestId, studentId, rowVersion, cancellationToken)); }
        catch (DbUpdateConcurrencyException) { return ServiceResult<bool>.Fail("conflict", "Request changed; please refresh."); }
    }

    private static string? Sanitize(string? value) => value is null ? null : WebUtility.HtmlEncode(value.Trim());
    private static StudentRequestResponse ToResponse(GuidanceRequestRecord x) => new(x.Id, x.Subject, x.Details, x.SafetyValveText, x.Urgency, x.Status, x.RowVersion);
}