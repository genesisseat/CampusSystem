using FinanceMain.Contracts;
using FinanceMain.Contracts.Dtos;

namespace FinanceMain.Services;

/// <summary>
/// In-memory synchronized store for Finance assessments, payments, clearances, and roster data.
/// Thread-safe and shared across Finance services.
/// </summary>
public sealed class FinanceDataStore
{
    private readonly object _lock = new();

    public static readonly Guid MariaClaraId = Guid.Parse("8c2f7bc9-31f2-4c36-9a4f-3f61d5da8b12");
    public static readonly Guid RenzoAguilarId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CelineBautistaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DanielleCruzId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid ElijahDomingoId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid FrancineEspinosaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid IanGonzalesId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid KatrinaHernandezId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid AlyssaMendozaId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    public record MutableAssessment(
        int Id,
        string AssessmentNumber,
        Guid StudentId,
        string StudentDisplayName,
        string StudentNumber,
        string Program,
        string AcademicYear,
        string Semester,
        decimal TotalAmount,
        decimal TotalPaid,
        string Status,
        DateTime AssessedDate,
        List<FeeAssessmentItemDto> Items)
    {
        public decimal TotalPaidAmount { get; set; } = TotalPaid;
        public string AssessmentStatus { get; set; } = Status;
        public decimal Balance => TotalAmount - TotalPaidAmount;
    }

    public record MutableClearance(
        int Id,
        Guid StudentId,
        string StudentDisplayName,
        string Program,
        string AcademicYear,
        string Semester,
        string Status,
        DateTime? IssuedDate,
        string? Remarks)
    {
        public string ClearanceStatus { get; set; } = Status;
        public DateTime? ClearedDate { get; set; } = IssuedDate;
        public string? Note { get; set; } = Remarks;
    }

    private readonly List<MutableAssessment> _assessments = [];
    private readonly List<PaymentRowDto> _payments = [];
    private readonly List<MutableClearance> _clearances = [];
    private readonly List<StudentRequestViewDto> _requests = [];

    public FinanceDataStore()
    {
        SeedData();
    }

    private void SeedData()
    {
        lock (_lock)
        {
            var mariaItems = new List<FeeAssessmentItemDto>
            {
                new("Tuition Fee (21 units)", 24000.00m, 12000.00m),
                new("Computer Laboratory Fee", 4500.00m, 4500.00m),
                new("Institutional Misc Fee", 3200.00m, 3200.00m),
                new("Registration Fee", 800.00m, 800.00m)
            };

            var standardItems = new List<FeeAssessmentItemDto>
            {
                new("Tuition Fee (21 units)", 24000.00m, 24000.00m),
                new("Computer Laboratory Fee", 4500.00m, 4500.00m),
                new("Institutional Misc Fee", 3200.00m, 3200.00m),
                new("Registration Fee", 800.00m, 800.00m)
            };

            _assessments.AddRange([
                new(1, "ASM-2026-00101", MariaClaraId, "Reyes, Maria Clara D.", "2023-00847", "BS Information Technology", "2026-2027", "1st Semester", 32500.00m, 20500.00m, "Partial", DateTime.UtcNow.AddDays(-20), mariaItems),
                new(2, "ASM-2026-00102", RenzoAguilarId, "Aguilar, Renzo Martin P.", "2023-00801", "BS Information Technology", "2026-2027", "1st Semester", 32500.00m, 32500.00m, "Paid", DateTime.UtcNow.AddDays(-22), standardItems),
                new(3, "ASM-2026-00103", CelineBautistaId, "Bautista, Celine Joy A.", "2023-00815", "BS Information Technology", "2026-2027", "1st Semester", 32500.00m, 32500.00m, "Paid", DateTime.UtcNow.AddDays(-25), standardItems),
                new(4, "ASM-2026-00104", DanielleCruzId, "Cruz, Danielle Mae S.", "2023-00822", "BS Information Technology", "2026-2027", "1st Semester", 32500.00m, 22000.00m, "Partial", DateTime.UtcNow.AddDays(-18), mariaItems),
                new(5, "ASM-2026-00105", ElijahDomingoId, "Domingo, Elijah James R.", "2023-00834", "BS Information Technology", "2026-2027", "1st Semester", 32500.00m, 5000.00m, "Unpaid", DateTime.UtcNow.AddDays(-15), [new("Tuition Deposit Fee", 32500.00m, 5000.00m)]),
                new(6, "ASM-2026-00106", FrancineEspinosaId, "Espinosa, Francine Nicole L.", "2023-00839", "BS Computer Science", "2026-2027", "1st Semester", 34000.00m, 34000.00m, "Paid", DateTime.UtcNow.AddDays(-24), standardItems),
                new(7, "ASM-2026-00107", IanGonzalesId, "Gonzales, Ian Carlo T.", "2023-00842", "BS Computer Science", "2026-2027", "1st Semester", 34000.00m, 8000.00m, "Unpaid", DateTime.UtcNow.AddDays(-12), [new("Tuition Deposit Fee", 34000.00m, 8000.00m)]),
                new(8, "ASM-2026-00108", KatrinaHernandezId, "Hernandez, Katrina Marie V.", "2023-00845", "BS Information Technology", "2026-2027", "1st Semester", 32500.00m, 32500.00m, "Paid", DateTime.UtcNow.AddDays(-28), standardItems),
                new(9, "ASM-2026-00109", AlyssaMendozaId, "Mendoza, Alyssa Bea R.", "2024-00101", "BS Information Technology", "2026-2027", "1st Semester", 32500.00m, 32500.00m, "Paid", DateTime.UtcNow.AddDays(-30), standardItems),
            ]);

            _payments.AddRange([
                new("PAY-2601-0089", DateTime.UtcNow.AddDays(-19), 12000.00m, "Bank Transfer (BDO)", "REF-9921448", "Success", "Reyes, Maria Clara D.", "BS Information Technology", 1, "ASM-2026-00101"),
                new("PAY-2601-0088", DateTime.UtcNow.AddDays(-20), 8500.00m, "GCash", "REF-8821033", "Success", "Reyes, Maria Clara D.", "BS Information Technology", 1, "ASM-2026-00101"),
                new("PAY-2601-0087", DateTime.UtcNow.AddDays(-22), 32500.00m, "Over-the-counter", "OR-440192", "Success", "Aguilar, Renzo Martin P.", "BS Information Technology", 2, "ASM-2026-00102"),
                new("PAY-2601-0086", DateTime.UtcNow.AddDays(-25), 32500.00m, "Online Banking", "REF-123490", "Success", "Bautista, Celine Joy A.", "BS Information Technology", 3, "ASM-2026-00103"),
                new("PAY-2601-0085", DateTime.UtcNow.AddDays(-18), 22000.00m, "Maya", "REF-771822", "Success", "Cruz, Danielle Mae S.", "BS Information Technology", 4, "ASM-2026-00104"),
                new("PAY-2601-0084", DateTime.UtcNow.AddDays(-24), 34000.00m, "Over-the-counter", "OR-440188", "Success", "Espinosa, Francine Nicole L.", "BS Computer Science", 6, "ASM-2026-00106"),
                new("PAY-2601-0083", DateTime.UtcNow.AddDays(-28), 32500.00m, "Online Banking", "REF-662910", "Success", "Hernandez, Katrina Marie V.", "BS Information Technology", 8, "ASM-2026-00108"),
            ]);

            _clearances.AddRange([
                new(1, MariaClaraId, "Reyes, Maria Clara D.", "BS Information Technology", "2026-2027", "1st Semester", "Conditional", null, "Promissory note approved for remaining balance"),
                new(2, RenzoAguilarId, "Aguilar, Renzo Martin P.", "BS Information Technology", "2026-2027", "1st Semester", "Cleared", DateTime.UtcNow.AddDays(-22), "Full payment received"),
                new(3, CelineBautistaId, "Bautista, Celine Joy A.", "BS Information Technology", "2026-2027", "1st Semester", "Cleared", DateTime.UtcNow.AddDays(-25), "Full payment received"),
                new(4, DanielleCruzId, "Cruz, Danielle Mae S.", "BS Information Technology", "2026-2027", "1st Semester", "Conditional", null, "Installment schedule verified"),
                new(5, ElijahDomingoId, "Domingo, Elijah James R.", "BS Information Technology", "2026-2027", "1st Semester", "NotCleared", null, null),
                new(6, FrancineEspinosaId, "Espinosa, Francine Nicole L.", "BS Computer Science", "2026-2027", "1st Semester", "Cleared", DateTime.UtcNow.AddDays(-24), "Full payment received"),
                new(7, IanGonzalesId, "Gonzales, Ian Carlo T.", "BS Computer Science", "2026-2027", "1st Semester", "NotCleared", null, null),
                new(8, KatrinaHernandezId, "Hernandez, Katrina Marie V.", "BS Information Technology", "2026-2027", "1st Semester", "Cleared", DateTime.UtcNow.AddDays(-28), "Full payment received"),
                new(9, AlyssaMendozaId, "Mendoza, Alyssa Bea R.", "BS Information Technology", "2026-2027", "1st Semester", "Cleared", DateTime.UtcNow.AddDays(-30), "Full payment received")
            ]);

            _requests.AddRange([
                new(Guid.NewGuid(), "REQ-260103-001", "Transcript of Records", 250m, "Released", [new StudentRequestPaymentDto("Paid")]),
                new(Guid.NewGuid(), "REQ-260218-004", "Certificate of Enrollment", 150m, "Processing", [new StudentRequestPaymentDto("Paid")]),
                new(Guid.NewGuid(), "REQ-260305-012", "Good Moral Certificate", 120m, "Pending", [new StudentRequestPaymentDto("Paid")])
            ]);
        }
    }

    public AssessmentListResult GetAssessments(AssessmentListFilter filter)
    {
        lock (_lock)
        {
            var query = _assessments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(a => a.AssessmentStatus.Equals(filter.Status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(a => a.StudentDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                         a.StudentNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                         a.AssessmentNumber.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var list = query.Select(ToDto).ToList();
            return new AssessmentListResult(list.Count, list);
        }
    }

    public FeeAssessmentDto? GetAssessmentById(int id)
    {
        lock (_lock)
        {
            var match = _assessments.FirstOrDefault(a => a.Id == id);
            return match != null ? ToDto(match) : null;
        }
    }

    public FeeAssessmentDto? GetCurrentAssessmentForStudent(Guid studentId, string academicYear, string semester)
    {
        lock (_lock)
        {
            var match = _assessments.FirstOrDefault(a => a.StudentId == studentId);
            return match != null ? ToDto(match) : null;
        }
    }

    public PaymentListResult GetPayments(PaymentListFilter filter)
    {
        lock (_lock)
        {
            var query = _payments.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(p => p.StudentDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                         p.PaymentNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                         p.AssessmentNumber.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var list = query.OrderByDescending(p => p.PaymentDate).ToList();
            return new PaymentListResult(list.Count, list);
        }
    }

    public IReadOnlyList<PaymentHistoryEntryDto> GetPaymentHistoryForStudent(Guid studentId)
    {
        lock (_lock)
        {
            var assessment = _assessments.FirstOrDefault(a => a.StudentId == studentId);
            if (assessment == null) return [];

            return _payments
                .Where(p => p.FeeAssessmentId == assessment.Id)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentHistoryEntryDto(p.PaymentNumber, p.PaymentDate, p.AmountPaid, p.PaymentMethod, p.ReferenceNumber, p.Status))
                .ToList();
        }
    }

    public PaymentRecordDto RecordPayment(RecordPaymentRequest request)
    {
        lock (_lock)
        {
            var assessment = _assessments.FirstOrDefault(a => a.Id == request.FeeAssessmentId);
            if (assessment == null)
            {
                throw new FinanceValidationException("Assessment record not found.");
            }

            if (request.AmountPaid <= 0)
            {
                throw new FinanceValidationException("Payment amount must be greater than zero.");
            }

            assessment.TotalPaidAmount += request.AmountPaid;
            if (assessment.Balance <= 0)
            {
                assessment.AssessmentStatus = "Paid";
            }
            else
            {
                assessment.AssessmentStatus = "Partial";
            }

            var payNo = $"PAY-{DateTime.UtcNow:yyMM}-{Random.Shared.Next(1000, 9999)}";
            var paymentRow = new PaymentRowDto(
                payNo,
                DateTime.UtcNow,
                request.AmountPaid,
                request.PaymentMethod,
                request.ReferenceNumber ?? $"REF-{Random.Shared.Next(100000, 999999)}",
                "Success",
                assessment.StudentDisplayName,
                assessment.Program,
                assessment.Id,
                assessment.AssessmentNumber);

            _payments.Insert(0, paymentRow);

            // Synchronize clearance status
            var clearance = _clearances.FirstOrDefault(c => c.StudentId == assessment.StudentId);
            if (clearance != null)
            {
                if (assessment.Balance <= 0)
                {
                    clearance.ClearanceStatus = "Cleared";
                    clearance.ClearedDate = DateTime.UtcNow;
                    clearance.Note = "Cleared upon full payment settlement.";
                }
                else
                {
                    clearance.ClearanceStatus = "Conditional";
                    clearance.Note = $"Partial payment of {request.AmountPaid:C} received.";
                }
            }

            return new PaymentRecordDto(payNo, request.AmountPaid, request.PaymentMethod);
        }
    }

    public ClearanceSummaryDto GetClearanceSummary()
    {
        lock (_lock)
        {
            var total = _clearances.Count;
            var cleared = _clearances.Count(c => c.ClearanceStatus == "Cleared");
            var cond = _clearances.Count(c => c.ClearanceStatus == "Conditional");
            var notCleared = _clearances.Count(c => c.ClearanceStatus == "NotCleared");
            return new ClearanceSummaryDto(total, cleared, cond, notCleared);
        }
    }

    public ClearanceListResult GetClearances(ClearanceListFilter filter)
    {
        lock (_lock)
        {
            var query = _clearances.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(c => c.ClearanceStatus.Equals(filter.Status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(c => c.StudentDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var list = query.Select(c =>
            {
                var asm = _assessments.FirstOrDefault(a => a.StudentId == c.StudentId);
                return new ClearanceRecordDto(
                    c.Id,
                    c.StudentDisplayName,
                    c.Program,
                    asm?.TotalAmount,
                    asm?.Balance,
                    c.ClearanceStatus,
                    c.ClearedDate);
            }).ToList();

            return new ClearanceListResult(list.Count, list);
        }
    }

    public void SetConditional(int clearanceId, string remarks)
    {
        lock (_lock)
        {
            var match = _clearances.FirstOrDefault(c => c.Id == clearanceId);
            if (match != null)
            {
                match.ClearanceStatus = "Conditional";
                match.Note = remarks;
            }
        }
    }

    public IReadOnlyList<StudentClearanceDto> GetStudentClearances(IReadOnlyCollection<Guid> studentIds)
    {
        lock (_lock)
        {
            return _clearances
                .Where(c => studentIds.Contains(c.StudentId))
                .Select(c => new StudentClearanceDto(c.StudentId, c.ClearanceStatus))
                .ToList();
        }
    }

    public StudentClearanceDto? GetStudentClearance(Guid studentId)
    {
        lock (_lock)
        {
            var match = _clearances.FirstOrDefault(c => c.StudentId == studentId);
            return match != null ? new StudentClearanceDto(match.StudentId, match.ClearanceStatus) : null;
        }
    }

    public IReadOnlyList<StudentRequestViewDto> GetRequests()
    {
        lock (_lock)
        {
            return _requests.ToList();
        }
    }

    public RequestCreateResultDto CreateRequest(CreateRequestRequest request)
    {
        lock (_lock)
        {
            var reqNo = $"REQ-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(100, 999)}";
            var created = new StudentRequestViewDto(
                Guid.NewGuid(),
                reqNo,
                request.DocumentType,
                250m * Math.Max(1, request.Copies),
                "Processing",
                [new StudentRequestPaymentDto("Paid")]);

            _requests.Insert(0, created);
            return new RequestCreateResultDto(created.Id, reqNo);
        }
    }

    public EnrolleeAssessmentResult AssessEnrollee(EnrolleeAssessmentRequest req)
    {
        lock (_lock)
        {
            var existing = _assessments.FirstOrDefault(a =>
                (a.StudentNumber.Equals(req.StudentNumber, StringComparison.OrdinalIgnoreCase) || a.StudentId == req.StudentId)
                && a.AcademicYear.Equals(req.AcademicYear, StringComparison.OrdinalIgnoreCase)
                && a.Semester.Equals(req.Semester, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                return new EnrolleeAssessmentResult(true, ToDto(existing));
            }

            var units = req.TotalUnits > 0 ? req.TotalUnits : 21;
            var tuitionFee = Math.Round(units * 1142.857m, 2);
            var labFee = 4500.00m;
            var miscFee = 3200.00m;
            var regFee = 800.00m;
            var totalAmount = tuitionFee + labFee + miscFee + regFee;

            var items = new List<FeeAssessmentItemDto>
            {
                new($"Tuition Fee ({units} units)", tuitionFee, 0m),
                new("Computer Laboratory Fee", labFee, 0m),
                new("Institutional Misc Fee", miscFee, 0m),
                new("Registration Fee", regFee, 0m)
            };

            var newId = _assessments.Count > 0 ? _assessments.Max(a => a.Id) + 1 : 1;
            var asmNo = $"ASM-{DateTime.UtcNow:yyyy}-{10100 + newId}";
            var newAsm = new MutableAssessment(
                newId,
                asmNo,
                req.StudentId,
                req.StudentName,
                req.StudentNumber,
                req.Program,
                req.AcademicYear,
                req.Semester,
                totalAmount,
                0m,
                "Unpaid",
                DateTime.UtcNow,
                items);

            _assessments.Insert(0, newAsm);

            var newClearanceId = _clearances.Count > 0 ? _clearances.Max(c => c.Id) + 1 : 1;
            _clearances.Add(new MutableClearance(
                newClearanceId,
                req.StudentId,
                req.StudentName,
                req.Program,
                req.AcademicYear,
                req.Semester,
                "NotCleared",
                null,
                "Awaiting matriculation settlement upon enrollment validation"));

            return new EnrolleeAssessmentResult(false, ToDto(newAsm));
        }
    }

    public PaymentRowDto? GetPaymentByNumber(string paymentNumber)
    {
        lock (_lock)
        {
            return _payments.FirstOrDefault(p => p.PaymentNumber.Equals(paymentNumber, StringComparison.OrdinalIgnoreCase));
        }
    }

    public int BatchIssueClearance(string academicYear, string semester)
    {
        lock (_lock)
        {
            int count = 0;
            foreach (var c in _clearances.Where(c => c.ClearanceStatus != "Cleared"))
            {
                var asm = _assessments.FirstOrDefault(a => a.StudentId == c.StudentId);
                if (asm != null && asm.Balance <= 0)
                {
                    c.ClearanceStatus = "Cleared";
                    c.ClearedDate = DateTime.UtcNow;
                    c.Note = "Batch clearance issued — balance fully settled.";
                    count++;
                }
            }
            return count;
        }
    }

    private static FeeAssessmentDto ToDto(MutableAssessment a) => new(
        a.Id,
        a.AssessmentNumber,
        a.StudentDisplayName,
        a.StudentNumber,
        a.Program,
        a.AcademicYear,
        a.Semester,
        a.TotalAmount,
        a.TotalPaidAmount,
        a.Balance,
        a.AssessmentStatus,
        a.AssessedDate,
        a.Items);
}

// ─── Active Services using FinanceDataStore ─────────────────────────────────

public sealed class ActiveAssessmentService(FinanceDataStore store) : IAssessmentService
{
    public Task<AssessmentListResult> ListAsync(AssessmentListFilter filter) =>
        Task.FromResult(store.GetAssessments(filter));

    public Task<FeeAssessmentDto?> GetByIdAsync(int id) =>
        Task.FromResult(store.GetAssessmentById(id));

    public Task<FeeAssessmentDto?> GetCurrentForStudentAsync(Guid studentId, string academicYear, string semester) =>
        Task.FromResult(store.GetCurrentAssessmentForStudent(studentId, academicYear, semester));

    public Task<EnrolleeAssessmentResult> AssessEnrolleeAsync(EnrolleeAssessmentRequest request) =>
        Task.FromResult(store.AssessEnrollee(request));
}

public sealed class ActivePaymentService(FinanceDataStore store, FinanceDbService? db = null) : IPaymentService
{
    public Task<PaymentListResult> ListAsync(PaymentListFilter filter) =>
        Task.FromResult(store.GetPayments(filter));

    public Task<IReadOnlyList<PaymentHistoryEntryDto>> GetPaymentHistoryForStudentAsync(Guid studentId) =>
        Task.FromResult(store.GetPaymentHistoryForStudent(studentId));

    public async Task<PaymentRecordDto> RecordPaymentAsync(RecordPaymentRequest request)
    {
        var record = store.RecordPayment(request);
        if (db != null)
        {
            try
            {
                var asm = store.GetAssessmentById(request.FeeAssessmentId);
                var isSettled = asm != null && asm.Balance <= 0;
                await db.RecordSharedPaymentAsync(
                    asm?.StudentNumber ?? "2024-00192",
                    asm?.StudentDisplayName ?? "Student",
                    record.PaymentNumber,
                    record.AmountPaid,
                    record.PaymentMethod,
                    $"Tuition Payment for Assessment {asm?.AssessmentNumber ?? "NUL"}",
                    isSettled);
            }
            catch
            {
                // Graceful degradation
            }
        }
        return record;
    }

    public Task<PaymentRowDto?> GetPaymentByNumberAsync(string paymentNumber) =>
        Task.FromResult(store.GetPaymentByNumber(paymentNumber));
}

public sealed class ActiveClearanceService(FinanceDataStore store, FinanceDbService? db = null) : IClearanceService
{
    public Task<ClearanceSummaryDto> GetSummaryAsync(string academicYear, string semester) =>
        Task.FromResult(store.GetClearanceSummary());

    public Task<ClearanceListResult> ListAsync(ClearanceListFilter filter) =>
        Task.FromResult(store.GetClearances(filter));

    public async Task SetConditionalAsync(int clearanceId, SetConditionalRequest request)
    {
        store.SetConditional(clearanceId, request.Remarks);
        if (db != null)
        {
            try
            {
                var list = store.GetClearances(new ClearanceListFilter("2026-2027", "1st Semester", null, null, 1, 100));
                var c = list.Items.FirstOrDefault(x => x.Id == clearanceId);
                if (c != null)
                {
                    await db.UpdateClearanceStatusAsync(c.StudentDisplayName, "Conditional", request.Remarks ?? "Conditional clearance");
                }
            }
            catch
            {
                // Graceful degradation
            }
        }
    }

    public Task<IReadOnlyList<StudentClearanceDto>> GetForStudentsAsync(IReadOnlyCollection<Guid> studentIds, string academicYear, string semester) =>
        Task.FromResult(store.GetStudentClearances(studentIds));

    public Task<StudentClearanceDto?> GetStatusAsync(Guid studentId, string academicYear, string semester) =>
        Task.FromResult(store.GetStudentClearance(studentId));

    public async Task<int> BatchIssueAsync(string academicYear, string semester)
    {
        var count = store.BatchIssueClearance(academicYear, semester);
        if (db != null)
        {
            try
            {
                var clearedList = store.GetClearances(new ClearanceListFilter(academicYear, semester, "Cleared", null, 1, 100));
                var asmList = store.GetAssessments(new AssessmentListFilter(academicYear, semester, null, null, 1, 100));
                foreach (var item in clearedList.Items)
                {
                    var asm = asmList.Items.FirstOrDefault(a => a.StudentDisplayName == item.StudentDisplayName);
                    if (asm != null)
                    {
                        await db.UpdateClearanceStatusAsync(asm.StudentNumber, "Cleared", "Full matriculation cleared & settled");
                    }
                }
            }
            catch
            {
                // Graceful degradation
            }
        }
        return count;
    }
}

public sealed class ActiveRequestService(FinanceDataStore store) : IRequestService
{
    private static readonly IReadOnlyList<DocumentFeeTypeDto> FeeTypes =
    [
        new(1, "Transcript of Records", 250m),
        new(2, "Certificate of Enrollment", 150m),
        new(3, "Diploma Copy", 350m),
        new(4, "Good Moral Certificate", 120m),
        new(5, "TOR with Authentication", 400m)
    ];

    public Task<IReadOnlyList<DocumentFeeTypeDto>> GetDocumentFeeTypesAsync() =>
        Task.FromResult(FeeTypes);

    public Task<RequestCreateResultDto> CreateRequestAsync(CreateRequestRequest request) =>
        Task.FromResult(store.CreateRequest(request));

    public Task PayRequestFeeAsync(Guid requestId, PayRequestFeeRequest request) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<StudentRequestViewDto>> GetRequestsForStudentAsync(Guid studentId) =>
        Task.FromResult(store.GetRequests());
}

public sealed class RegistrarClassRosterProvider : IClassRosterProvider
{
    public Task<ClassRosterDto?> GetRosterAsync(string sectionId)
    {
        var roster = new ClassRosterDto(
            SectionLabel: $"{sectionId} · MWF 8:00–9:30 · Room 402",
            Students:
            [
                new(FinanceDataStore.RenzoAguilarId, "Aguilar, Renzo Martin P.", "2023-00801"),
                new(FinanceDataStore.CelineBautistaId, "Bautista, Celine Joy A.", "2023-00815"),
                new(FinanceDataStore.DanielleCruzId, "Cruz, Danielle Mae S.", "2023-00822"),
                new(FinanceDataStore.ElijahDomingoId, "Domingo, Elijah James R.", "2023-00834"),
                new(FinanceDataStore.FrancineEspinosaId, "Espinosa, Francine Nicole L.", "2023-00839"),
                new(FinanceDataStore.IanGonzalesId, "Gonzales, Ian Carlo T.", "2023-00842"),
                new(FinanceDataStore.KatrinaHernandezId, "Hernandez, Katrina Marie V.", "2023-00845"),
                new(FinanceDataStore.MariaClaraId, "Reyes, Maria Clara D.", "2023-00847")
            ]
        );

        return Task.FromResult<ClassRosterDto?>(roster);
    }
}

// ─── Legacy NoOp classes preserved for backward compatibility ───────────────

public sealed class NoOpAssessmentService : IAssessmentService
{
    public Task<AssessmentListResult> ListAsync(AssessmentListFilter filter) =>
        Task.FromResult(new AssessmentListResult(0, Array.Empty<FeeAssessmentDto>()));

    public Task<FeeAssessmentDto?> GetByIdAsync(int id) => Task.FromResult<FeeAssessmentDto?>(null);

    public Task<FeeAssessmentDto?> GetCurrentForStudentAsync(Guid studentId, string academicYear, string semester) =>
        Task.FromResult<FeeAssessmentDto?>(null);

    public Task<EnrolleeAssessmentResult> AssessEnrolleeAsync(EnrolleeAssessmentRequest request) =>
        Task.FromResult(new EnrolleeAssessmentResult(false, new FeeAssessmentDto(
            0, "ASM-0000", request.StudentName, request.StudentNumber, request.Program, request.AcademicYear, request.Semester, 0m, 0m, 0m, "Unpaid", DateTime.UtcNow, Array.Empty<FeeAssessmentItemDto>())));
}

public sealed class NoOpPaymentService : IPaymentService
{
    public Task<PaymentListResult> ListAsync(PaymentListFilter filter) =>
        Task.FromResult(new PaymentListResult(0, Array.Empty<PaymentRowDto>()));

    public Task<IReadOnlyList<PaymentHistoryEntryDto>> GetPaymentHistoryForStudentAsync(Guid studentId) =>
        Task.FromResult<IReadOnlyList<PaymentHistoryEntryDto>>(Array.Empty<PaymentHistoryEntryDto>());

    public Task<PaymentRecordDto> RecordPaymentAsync(RecordPaymentRequest request) =>
        Task.FromResult(new PaymentRecordDto("PAY-0000", request.AmountPaid, request.PaymentMethod));

    public Task<PaymentRowDto?> GetPaymentByNumberAsync(string paymentNumber) =>
        Task.FromResult<PaymentRowDto?>(null);
}

public sealed class NoOpClearanceService : IClearanceService
{
    public Task<ClearanceSummaryDto> GetSummaryAsync(string academicYear, string semester) =>
        Task.FromResult(new ClearanceSummaryDto(0, 0, 0, 0));

    public Task<ClearanceListResult> ListAsync(ClearanceListFilter filter) =>
        Task.FromResult(new ClearanceListResult(0, Array.Empty<ClearanceRecordDto>()));

    public Task SetConditionalAsync(int clearanceId, SetConditionalRequest request) => Task.CompletedTask;

    public Task<IReadOnlyList<StudentClearanceDto>> GetForStudentsAsync(IReadOnlyCollection<Guid> studentIds, string academicYear, string semester) =>
        Task.FromResult<IReadOnlyList<StudentClearanceDto>>(Array.Empty<StudentClearanceDto>());

    public Task<StudentClearanceDto?> GetStatusAsync(Guid studentId, string academicYear, string semester) =>
        Task.FromResult<StudentClearanceDto?>(null);

    public Task<int> BatchIssueAsync(string academicYear, string semester) =>
        Task.FromResult(0);
}

public sealed class NoOpRequestService : IRequestService
{
    private static readonly Guid DemoStudentId = Guid.Parse("8c2f7bc9-31f2-4c36-9a4f-3f61d5da8b12");

    private static readonly IReadOnlyList<DocumentFeeTypeDto> DemoDocumentFeeTypes =
    [
        new(1, "Transcript of Records", 250m),
        new(2, "Certificate of Enrollment", 150m),
        new(3, "Diploma Copy", 350m),
        new(4, "Good Moral Certificate", 120m),
        new(5, "TOR with Authentication", 400m)
    ];

    public Task<IReadOnlyList<DocumentFeeTypeDto>> GetDocumentFeeTypesAsync() =>
        Task.FromResult(DemoDocumentFeeTypes);

    public Task<RequestCreateResultDto> CreateRequestAsync(CreateRequestRequest request) =>
        Task.FromResult(new RequestCreateResultDto(Guid.NewGuid(), $"REQ-{DateTime.UtcNow:yyMMddHHmmss}"));

    public Task PayRequestFeeAsync(Guid requestId, PayRequestFeeRequest request) => Task.CompletedTask;

    public Task<IReadOnlyList<StudentRequestViewDto>> GetRequestsForStudentAsync(Guid studentId)
    {
        var effectiveStudentId = studentId == Guid.Empty ? DemoStudentId : studentId;

        var requests = new[]
        {
            new StudentRequestViewDto(
                Guid.NewGuid(),
                "REQ-240103-001",
                "Transcript of Records",
                300m,
                "Released",
                [new StudentRequestPaymentDto("Paid")]),
            new StudentRequestViewDto(
                Guid.NewGuid(),
                "REQ-240218-004",
                "Certificate of Enrollment",
                200m,
                "Processing",
                [new StudentRequestPaymentDto("Paid")])
        };

        return Task.FromResult<IReadOnlyList<StudentRequestViewDto>>(
            effectiveStudentId == DemoStudentId ? requests : Array.Empty<StudentRequestViewDto>());
    }
}

public sealed class PendingClassRosterProvider : IClassRosterProvider
{
    public Task<ClassRosterDto?> GetRosterAsync(string sectionId) => Task.FromResult<ClassRosterDto?>(null);
}
