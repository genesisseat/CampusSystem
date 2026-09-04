using FinanceMain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FinanceMain.Services;

public sealed class CounselorTriageService(IGuidanceRequestStore store, IAuditLogService audit) : ICounselorTriageService
{
    public async Task<IReadOnlyList<StudentRequestResponse>> ListAsync(TriageFilter filter, CancellationToken cancellationToken) => (await store.ListAsync(null, filter, cancellationToken)).Select(ToResponse).ToList();

    public async Task<ServiceResult<StudentRequestResponse>> TransitionAsync(string actorId, TransitionRequest transition, CancellationToken cancellationToken)
    {
        var request = await store.FindAsync(transition.RequestId, cancellationToken);
        if (request is null) return ServiceResult<StudentRequestResponse>.Fail("not_found", "Request not found.");
        if (!IsValid(request.Status, transition.TargetStatus)) return ServiceResult<StudentRequestResponse>.Fail("invalid_transition", "The requested state transition is not allowed.");
        var before = request.Status.ToString(); request.Status = transition.TargetStatus;
        try { await store.SaveAsync(request, transition.RowVersion, cancellationToken); }
        catch (DbUpdateConcurrencyException) { return ServiceResult<StudentRequestResponse>.Fail("conflict", "Record changed; please refresh."); }
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), actorId, "status_transition", "GuidanceRequest", request.Id.ToString(), before, request.Status.ToString(), DateTimeOffset.UtcNow), cancellationToken);
        return ServiceResult<StudentRequestResponse>.Ok(ToResponse(request));
    }

    private static bool IsValid(RequestStatus current, RequestStatus next) => (current, next) is (RequestStatus.Requested, RequestStatus.InProgress) or (RequestStatus.InProgress, RequestStatus.Resolved);
    private static StudentRequestResponse ToResponse(GuidanceRequestRecord x) => new(x.Id, x.Subject, x.Details, x.SafetyValveText, x.Urgency, x.Status, x.RowVersion);
}
