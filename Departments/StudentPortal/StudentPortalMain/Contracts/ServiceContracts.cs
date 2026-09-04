using System.Security.Claims;

namespace StudentPortalMain.Contracts;

public record ServiceResult<T>(bool Succeeded, T? Value, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static ServiceResult<T> Ok(T value) => new(true, value);
    public static ServiceResult<T> Fail(string code, string message) => new(false, default, code, message);
}

public enum RequestStatus { Requested, InProgress, Resolved }
public enum RequestUrgency { Normal, Urgent }

public record StudentRequestDto(string Subject, string Details, string? SafetyValveText, RequestUrgency Urgency);
public record StudentRequestResponse(Guid Id, string Subject, string Details, string? SafetyValveText, RequestUrgency Urgency, RequestStatus Status, byte[] RowVersion);
public record TriageFilter(RequestStatus? Status, RequestUrgency? Urgency, Guid? AssignedCounselorId);
public record TransitionRequest(Guid RequestId, RequestStatus TargetStatus, byte[] RowVersion);
public record AuditEvent(Guid Id, string ActorId, string Action, string Entity, string EntityId, string? BeforeState, string? AfterState, DateTimeOffset Timestamp);
public record AuthRequest(string UserName, string Password, string Role);
public record AuthResponse(string Role, DateTimeOffset ExpiresAt);
public record ImportRowError(int Row, string Field, string Message);
public record ImportValidationResult(bool Passed, int RowCount, IReadOnlyList<ImportRowError> Errors, IReadOnlyList<string> SchemaDrift);
public record NotificationMessage(string Recipient, string Subject, string Body);
public record RedactionRules(bool Emails = true, bool PhoneNumbers = true, bool SsnLikeValues = true);

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetStudentId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("StudentId"), out var id) ? id : null;
}
