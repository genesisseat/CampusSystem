using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusSystem.Data.Services;
using FinanceMain.Contracts.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceMain.Services;

public class FinanceDbService
{
    private readonly CampusJsonDb _jsonDb;
    private readonly ILogger<FinanceDbService> _logger;

    public FinanceDbService(IConfiguration config, ILogger<FinanceDbService> logger, CampusJsonDb jsonDb)
    {
<<<<<<< Updated upstream
        _connectionString = config.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
=======
>>>>>>> Stashed changes
        _logger = logger;
        _jsonDb = jsonDb;
    }

    public Task EnsureSchemaAsync()
    {
<<<<<<< Updated upstream
        try
        {
            await using var db = CreateConnection();
            await db.OpenAsync();

            // Ensure student_payments table (compatible with StudentPortal)
            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `student_payments` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id` INT NOT NULL,
                    `receipt_no` VARCHAR(60) NOT NULL UNIQUE,
                    `amount` DECIMAL(10,2) NOT NULL,
                    `pay_date` DATETIME NOT NULL,
                    `payment_method` VARCHAR(50) NOT NULL,
                    `status` VARCHAR(30) NOT NULL DEFAULT 'Paid',
                    `description` VARCHAR(255) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            // Ensure student_clearance table (compatible with StudentPortal, Registrar & Faculty)
            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `student_clearance` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id` INT NOT NULL,
                    `department_name` VARCHAR(100) NOT NULL,
                    `school_year` VARCHAR(50) NOT NULL,
                    `semester` VARCHAR(50) NOT NULL,
                    `status` VARCHAR(50) NOT NULL DEFAULT 'Cleared',
                    `cleared_by` VARCHAR(150) NOT NULL DEFAULT 'Dean / Head',
                    `cleared_at` DATETIME NULL,
                    `remarks` TEXT NULL,
                    UNIQUE KEY `uniq_stud_dept` (`student_id`, `department_name`, `school_year`, `semester`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            // Ensure fee_assessments table
            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `fee_assessments` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `assessment_number` VARCHAR(60) NOT NULL UNIQUE,
                    `student_id` INT NOT NULL,
                    `student_number` VARCHAR(50) NOT NULL,
                    `student_name` VARCHAR(150) NOT NULL,
                    `program` VARCHAR(100) NOT NULL,
                    `school_year` VARCHAR(50) NOT NULL,
                    `semester` VARCHAR(50) NOT NULL,
                    `total_amount` DECIMAL(10,2) NOT NULL,
                    `total_paid` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                    `status` VARCHAR(30) NOT NULL DEFAULT 'Unpaid',
                    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            _logger.LogInformation("Finance MySQL schema verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not initialize the Finance MySQL schema.");
            throw;
        }
=======
        return Task.CompletedTask;
>>>>>>> Stashed changes
    }

    public Task<int?> FindStudentIdByNumberAsync(string studentNumber)
    {
<<<<<<< Updated upstream
        try
        {
            await using var db = CreateConnection();
            return await db.QueryFirstOrDefaultAsync<int?>(
                "SELECT id FROM `user` WHERE student_id_number = @studentNumber LIMIT 1",
                new { studentNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not resolve student {StudentNumber} from the shared MySQL database.", studentNumber);
            throw;
        }
=======
        var users = _jsonDb.GetUsers();
        var u = users.FirstOrDefault(x => string.Equals(x.StudentIdNumber, studentNumber, StringComparison.OrdinalIgnoreCase) && x.Role == "student");
        return Task.FromResult<int?>(u?.Id);
>>>>>>> Stashed changes
    }

    public Task<List<DbAssessmentRow>> GetAllAssessmentsFromDbAsync(string? status = null, string? search = null)
    {
        var assessments = _jsonDb.GetFeeAssessments();
        var users = _jsonDb.GetUsers();
        var profiles = _jsonDb.GetStudentProfiles();
        var list = new List<DbAssessmentRow>();

        foreach (var a in assessments)
        {
            var u = users.FirstOrDefault(x => x.Id == a.StudentId);
            var prof = profiles.FirstOrDefault(x => x.StudentId == a.StudentId);
            decimal balance = Math.Max(0, a.TotalAmount - a.TotalPaid);
            string rowStatus = balance <= 0 ? "Paid" : (a.TotalPaid > 0 ? "Partial" : "Unpaid");

            if (!string.IsNullOrEmpty(status) && !string.Equals(status, rowStatus, StringComparison.OrdinalIgnoreCase))
                continue;

            string name = u?.Name ?? a.StudentName;
            string snum = u?.StudentIdNumber ?? a.StudentNumber;
            string asmnum = a.AssessmentNumber;

            if (!string.IsNullOrEmpty(search))
            {
                bool match = (name.Contains(search, StringComparison.OrdinalIgnoreCase))
                          || (snum.Contains(search, StringComparison.OrdinalIgnoreCase))
                          || (asmnum.Contains(search, StringComparison.OrdinalIgnoreCase));
                if (!match) continue;
            }

            list.Add(new DbAssessmentRow(
                a.Id,
                asmnum,
                a.StudentId,
                snum,
                name,
                prof?.Program ?? a.Program,
                a.SchoolYear,
                a.Semester,
                a.TotalAmount,
                a.TotalPaid,
                rowStatus,
                a.CreatedAt));
        }

        return Task.FromResult(list);
    }

    public Task<List<DbPaymentRow>> GetAllPaymentsFromDbAsync(string? search = null)
    {
        var payments = _jsonDb.GetStudentPayments();
        var users = _jsonDb.GetUsers();
        var profiles = _jsonDb.GetStudentProfiles();
        var list = new List<DbPaymentRow>();

        foreach (var p in payments)
        {
            var u = users.FirstOrDefault(x => x.Id == p.StudentId);
            var prof = profiles.FirstOrDefault(x => x.StudentId == p.StudentId);

            string name = u?.Name ?? p.StudentName;
            string snum = u?.StudentIdNumber ?? p.StudentNumber;
            string rcpt = p.ReceiptNo;

            if (!string.IsNullOrEmpty(search))
            {
                bool match = name.Contains(search, StringComparison.OrdinalIgnoreCase)
                          || snum.Contains(search, StringComparison.OrdinalIgnoreCase)
                          || rcpt.Contains(search, StringComparison.OrdinalIgnoreCase);
                if (!match) continue;
            }

            list.Add(new DbPaymentRow(
                PaymentNumber: p.ReceiptNo,
                PaymentDate: p.PayDate,
                AmountPaid: p.Amount,
                PaymentMethod: p.PaymentMethod,
                Status: "Paid",
                StudentDisplayName: name,
                Program: prof?.Program ?? "BS Information Technology",
                AssessmentNumber: $"ASM-2026-{p.StudentId:D5}",
                FeeAssessmentId: p.FeeAssessmentId,
                StudentNumber: snum));
        }

        return Task.FromResult(list);
    }

    public Task<List<DbClearanceRow>> GetAllClearancesFromDbAsync(string? status = null, string? search = null)
    {
        var clearances = _jsonDb.GetStudentClearances();
        var users = _jsonDb.GetUsers();
        var profiles = _jsonDb.GetStudentProfiles();
        var assessments = _jsonDb.GetFeeAssessments();
        var list = new List<DbClearanceRow>();

        var finClearances = clearances.Where(x => x.DepartmentName == "Finance" || x.DepartmentName.Contains("Finance")).ToList();

        foreach (var c in finClearances)
        {
            var u = users.FirstOrDefault(x => x.Id == c.StudentId);
            var prof = profiles.FirstOrDefault(x => x.StudentId == c.StudentId);
            var asm = assessments.FirstOrDefault(x => x.StudentId == c.StudentId);

            decimal assessed = asm?.TotalAmount ?? 36000m;
            decimal paid = asm?.TotalPaid ?? (c.Status == "Cleared" ? 36000m : 0m);

            if (!string.IsNullOrEmpty(status) && !string.Equals(status, c.Status, StringComparison.OrdinalIgnoreCase))
                continue;

            string name = u?.Name ?? c.StudentName;
            string snum = u?.StudentIdNumber ?? c.StudentNumber;

            if (!string.IsNullOrEmpty(search))
            {
                bool match = name.Contains(search, StringComparison.OrdinalIgnoreCase)
                          || snum.Contains(search, StringComparison.OrdinalIgnoreCase);
                if (!match) continue;
            }

            list.Add(new DbClearanceRow(
                Id: c.Id,
                StudentId: c.StudentId,
                StudentName: name,
                StudentNumber: snum,
                Program: prof?.Program ?? "BS Information Technology",
                Assessed: assessed,
                Balance: Math.Max(0, assessed - paid),
                Status: c.Status,
                ClearedOn: c.Status == "Cleared" ? c.UpdatedAt : null,
                Remarks: c.Remarks));
        }

        return Task.FromResult(list);
    }

    public Task<bool> RecordSharedPaymentAsync(
        string studentNumber,
        string studentName,
        string receiptNumber,
        decimal amount,
        string paymentMethod,
        string description,
        bool isFullySettled = false)
    {
        var users = _jsonDb.GetUsers();
        var u = users.FirstOrDefault(x => string.Equals(x.StudentIdNumber, studentNumber, StringComparison.OrdinalIgnoreCase));
        int studentId = u?.Id ?? 5;

<<<<<<< Updated upstream
            var studentId = await FindStudentIdByNumberAsync(studentNumber);
            if (studentId is null)
            {
                _logger.LogWarning("Payment {Receipt} rejected: student {StudentNumber} was not found.", receiptNumber, studentNumber);
                return false;
            }

            // 1. Insert into student_payments (immediately visible in Student Portal)
            await db.ExecuteAsync(@"
                INSERT INTO `student_payments` 
                (`student_id`, `receipt_no`, `amount`, `pay_date`, `payment_method`, `status`, `description`) 
                VALUES (@studentId, @receiptNumber, @amount, NOW(), @paymentMethod, 'Paid', @description)
                ON DUPLICATE KEY UPDATE `amount` = @amount, `status` = 'Paid';",
                new { studentId, receiptNumber, amount, paymentMethod, description });

            // 2. If fully settled, update Finance & Accounting Office clearance row
            if (isFullySettled)
            {
                await db.ExecuteAsync(@"
                    INSERT INTO `student_clearance` 
                    (`student_id`, `department_name`, `school_year`, `semester`, `status`, `cleared_by`, `cleared_at`, `remarks`) 
                    VALUES (@studentId, 'Finance & Accounting Office', '2026-2027', '1st Semester', 'Cleared', 'Ms. Carmela Ilagan', NOW(), 'Full matriculation cleared & settled')
                    ON DUPLICATE KEY UPDATE `status` = 'Cleared', `cleared_at` = NOW(), `remarks` = 'Full matriculation cleared & settled';",
                    new { studentId });
            }

            _logger.LogInformation("Payment {Receipt} (₱{Amount}) synchronized to shared MySQL database.", receiptNumber, amount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to record shared MySQL payment: {Message}", ex.Message);
            return false;
        }
=======
        _jsonDb.RecordPayment(studentId, amount, paymentMethod, receiptNumber);
        return Task.FromResult(true);
>>>>>>> Stashed changes
    }

    public Task<bool> UpdateClearanceStatusAsync(string studentNumber, string status, string remarks)
    {
        var clearances = _jsonDb.GetStudentClearances();
        var clr = clearances.FirstOrDefault(c => string.Equals(c.StudentNumber, studentNumber, StringComparison.OrdinalIgnoreCase) && c.DepartmentName.Contains("Finance"));
        if (clr != null)
        {
            clr.Status = status;
            clr.Remarks = remarks;
            clr.UpdatedAt = DateTime.UtcNow;
            _jsonDb.SaveStudentClearances(clearances);
        }
        return Task.FromResult(true);
    }

    public Task<List<ClassRosterStudentDto>> GetClassRosterFromDbAsync(string sectionId)
    {
        var users = _jsonDb.GetUsers().Where(u => u.Role == "student").ToList();
        var list = new List<ClassRosterStudentDto>();

        foreach (var s in users)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(s.Id).CopyTo(bytes, 0);
            var guid = new Guid(bytes);
            list.Add(new ClassRosterStudentDto(guid, s.Name, s.StudentIdNumber));
        }

        return Task.FromResult(list);
    }

    public async Task<FeeAssessmentDto?> GetAssessmentByIdFromDbAsync(int id)
    {
        var all = await GetAllAssessmentsFromDbAsync();
        var row = all.FirstOrDefault(a => a.Id == id || a.StudentId == id);
        if (row == null) return null;

        return new FeeAssessmentDto(
            row.Id,
            row.AssessmentNumber,
            row.StudentName,
            row.StudentNumber,
            row.Program,
            row.SchoolYear,
            row.Semester,
            row.TotalAmount,
            row.TotalPaid,
            row.Balance,
            row.Status,
            row.CreatedAt,
            new List<FeeAssessmentItemDto>
            {
                new("Tuition Fee", Math.Max(0, row.TotalAmount - 4500.00m), Math.Min(Math.Max(0, row.TotalAmount - 4500.00m), row.TotalPaid)),
                new("Miscellaneous & Tech Fee", 4500.00m, Math.Max(0, row.TotalPaid - Math.Max(0, row.TotalAmount - 4500.00m)))
            });
    }

    public async Task<FeeAssessmentDto?> GetCurrentAssessmentForStudentFromDbAsync(Guid studentId, string academicYear, string semester)
    {
        var all = await GetAllAssessmentsFromDbAsync();
        byte[] bytes = studentId.ToByteArray();
        int intId = BitConverter.ToInt32(bytes, 0);
        var row = all.FirstOrDefault(a => a.StudentId == intId) ?? all.FirstOrDefault();
        if (row == null) return null;

        return new FeeAssessmentDto(
            row.Id,
            row.AssessmentNumber,
            row.StudentName,
            row.StudentNumber,
            row.Program,
            row.SchoolYear,
            row.Semester,
            row.TotalAmount,
            row.TotalPaid,
            row.Balance,
            row.Status,
            row.CreatedAt,
            new List<FeeAssessmentItemDto>
            {
                new("Tuition Fee", Math.Max(0, row.TotalAmount - 4500.00m), Math.Min(Math.Max(0, row.TotalAmount - 4500.00m), row.TotalPaid)),
                new("Miscellaneous & Tech Fee", 4500.00m, Math.Max(0, row.TotalPaid - Math.Max(0, row.TotalAmount - 4500.00m)))
            });
    }

    public async Task<PaymentRowDto?> GetPaymentByNumberFromDbAsync(string paymentNumber)
    {
        var all = await GetAllPaymentsFromDbAsync();
        var p = all.FirstOrDefault(x => string.Equals(x.PaymentNumber, paymentNumber, StringComparison.OrdinalIgnoreCase));
        if (p == null) return null;

        return new PaymentRowDto(
            p.PaymentNumber,
            p.PaymentDate,
            p.AmountPaid,
            p.PaymentMethod,
            p.PaymentNumber,
            p.Status,
            p.StudentDisplayName,
            p.Program,
            p.FeeAssessmentId,
            p.AssessmentNumber);
    }
}

// ─── Internal DTO records used by FinancePlaceholderServices ─────────────────

public record DbAssessmentRow(
    int Id,
    string AssessmentNumber,
    int StudentId,
    string StudentNumber,
    string StudentName,
    string Program,
    string SchoolYear,
    string Semester,
    decimal TotalAmount,
    decimal TotalPaid,
    string Status,
    DateTime CreatedAt)
{
    public decimal Balance => TotalAmount - TotalPaid;
}

public record DbPaymentRow(
    string PaymentNumber,
    DateTime PaymentDate,
    decimal AmountPaid,
    string PaymentMethod,
    string Status,
    string StudentDisplayName,
    string Program,
    string AssessmentNumber,
    int FeeAssessmentId,
    string StudentNumber);

public record DbClearanceRow(
    int Id,
    int StudentId,
    string StudentName,
    string StudentNumber,
    string Program,
    decimal Assessed,
    decimal Balance,
    string Status,
    DateTime? ClearedOn,
    string? Remarks);
