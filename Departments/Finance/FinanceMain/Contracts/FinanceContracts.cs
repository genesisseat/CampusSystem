using FinanceMain.Contracts.Dtos;

namespace FinanceMain.Contracts;

public class FinanceValidationException : Exception
{
    public FinanceValidationException(string message) : base(message) { }
    public FinanceValidationException(string message, Exception innerException) : base(message, innerException) { }
}

public record AssessmentListFilter(
    string AcademicYear,
    string Semester,
    string? Status,
    string? SearchText,
    int Page,
    int PageSize);

public record ClearanceListFilter(
    string AcademicYear,
    string Semester,
    string? Status,
    string? SearchText,
    int Page,
    int PageSize);

public record PaymentListFilter(string? SearchText, int Page, int PageSize);

public record SetConditionalRequest(string Remarks);

public record RecordPaymentRequest(int FeeAssessmentId, decimal AmountPaid, string PaymentMethod, string? ReferenceNumber);

public record CreateRequestRequest(Guid StudentId, string DocumentType, int DocumentFeeTypeId, int Copies, string? Purpose);

public record PayRequestFeeRequest(string PaymentMethod);

public record EnrolleeAssessmentRequest(
    Guid StudentId,
    string StudentNumber,
    string StudentName,
    string Program,
    string AcademicYear,
    string Semester,
    int TotalUnits);

public record EnrolleeAssessmentResult(
    bool IsExisting,
    FeeAssessmentDto Assessment);

public interface IAssessmentService
{
    Task<AssessmentListResult> ListAsync(AssessmentListFilter filter);
    Task<FeeAssessmentDto?> GetByIdAsync(int id);
    Task<FeeAssessmentDto?> GetCurrentForStudentAsync(Guid studentId, string academicYear, string semester);
    Task<EnrolleeAssessmentResult> AssessEnrolleeAsync(EnrolleeAssessmentRequest request);
}

public interface IPaymentService
{
    Task<PaymentListResult> ListAsync(PaymentListFilter filter);
    Task<IReadOnlyList<PaymentHistoryEntryDto>> GetPaymentHistoryForStudentAsync(Guid studentId);
    Task<PaymentRecordDto> RecordPaymentAsync(RecordPaymentRequest request);
    Task<PaymentRowDto?> GetPaymentByNumberAsync(string paymentNumber);
}

public interface IClearanceService
{
    Task<ClearanceSummaryDto> GetSummaryAsync(string academicYear, string semester);
    Task<ClearanceListResult> ListAsync(ClearanceListFilter filter);
    Task SetConditionalAsync(int clearanceId, SetConditionalRequest request);
    Task<IReadOnlyList<StudentClearanceDto>> GetForStudentsAsync(IReadOnlyCollection<Guid> studentIds, string academicYear, string semester);
    Task<StudentClearanceDto?> GetStatusAsync(Guid studentId, string academicYear, string semester);
    Task<int> BatchIssueAsync(string academicYear, string semester);
}

public interface IRequestService
{
    Task<IReadOnlyList<DocumentFeeTypeDto>> GetDocumentFeeTypesAsync();
    Task<RequestCreateResultDto> CreateRequestAsync(CreateRequestRequest request);
    Task PayRequestFeeAsync(Guid requestId, PayRequestFeeRequest request);
    Task<IReadOnlyList<StudentRequestViewDto>> GetRequestsForStudentAsync(Guid studentId);
}

public interface IClassRosterProvider
{
    Task<ClassRosterDto?> GetRosterAsync(string sectionId);
}

public record RequestCreateResultDto(Guid Id, string RequestNumber);
