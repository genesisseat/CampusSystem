using System;
using System.Collections.Generic;

namespace CampusSystem.Data.Models;

public class CampusUser
{
    public int Id { get; set; }
    public string StudentIdNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "student"; // student, faculty, registrar, finance, guidance, admin
    public string Status { get; set; } = "active";
    public string AvatarUrl { get; set; } = "";
}

public class StudentProfileRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string Program { get; set; } = "BS Information Technology";
    public int YearLevel { get; set; } = 2;
    public string AcademicStatus { get; set; } = "Regular";
    public decimal Gwa { get; set; } = 1.75m;
    public string ContactNumber { get; set; } = "0917-123-4567";
    public string Address { get; set; } = "Manila, Philippines";
}

public class SubjectRecord
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public int Units { get; set; } = 3;
    public int LectureHours { get; set; } = 3;
    public int LabHours { get; set; } = 0;
    public string Prerequisite { get; set; } = "None";
}

public class ClassOfferingRecord
{
    public int Id { get; set; }
    public string SectionCode { get; set; } = "";
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = "";
    public string SubjectTitle { get; set; } = "";
    public int Units { get; set; } = 3;
    public string Room { get; set; } = "";
    public string DaysOfWeek { get; set; } = "MWF";
    public string StartTime { get; set; } = "08:00 AM";
    public string EndTime { get; set; } = "09:30 AM";
    public string InstructorName { get; set; } = "";
    public int MaxSlots { get; set; } = 40;
    public int SlotsTaken { get; set; } = 0;
}

public class EnrollmentRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Program { get; set; } = "";
    public string SchoolYear { get; set; } = "2026-2027";
    public string Semester { get; set; } = "1st Semester";
    public string EnrollmentType { get; set; } = "Regular";
    public string Status { get; set; } = "pending"; // pending, active, enrolled, reserved, rejected
    public int TotalUnits { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
}

public class EnrolledSubjectRecord
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int ClassOfferingId { get; set; }
    public string SubjectCode { get; set; } = "";
    public string SubjectTitle { get; set; } = "";
    public int Units { get; set; } = 3;
    public string Status { get; set; } = "enrolled"; // enrolled, dropped
    public string? Grade { get; set; }
}

public class FeeAssessmentRecord
{
    public int Id { get; set; }
    public string AssessmentNumber { get; set; } = "";
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Program { get; set; } = "";
    public string SchoolYear { get; set; } = "2026-2027";
    public string Semester { get; set; } = "1st Semester";
    public int TotalUnits { get; set; } = 0;
    public decimal TuitionFee { get; set; } = 0m;
    public decimal MiscellaneousFee { get; set; } = 4500m;
    public decimal TotalAmount { get; set; } = 0m;
    public decimal TotalPaid { get; set; } = 0m;
    public decimal Balance => Math.Max(0m, TotalAmount - TotalPaid);
    public string Status { get; set; } = "Unpaid"; // Unpaid, Partially Paid, Fully Paid
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StudentPaymentRecord
{
    public int Id { get; set; }
    public string ReceiptNo { get; set; } = "";
    public int FeeAssessmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Online Banking";
    public string ReferenceNumber { get; set; } = "";
    public DateTime PayDate { get; set; } = DateTime.UtcNow;
    public string SchoolYear { get; set; } = "2026-2027";
    public string Semester { get; set; } = "1st Semester";
}

public class StudentClearanceRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string DepartmentName { get; set; } = ""; // Finance, Registrar, Library, Guidance
    public string Status { get; set; } = "Pending"; // Cleared, Pending, Hold
    public string SchoolYear { get; set; } = "2026-2027";
    public string Semester { get; set; } = "1st Semester";
    public string Remarks { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class AttendanceRecordItem
{
    public int Id { get; set; }
    public int ClassOfferingId { get; set; }
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public DateTime SessionDate { get; set; } = DateTime.UtcNow.Date;
    public string Status { get; set; } = "Present"; // Present, Absent, Late, Excused
    public string Topic { get; set; } = "";
}

public class GradeRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int ClassOfferingId { get; set; }
    public string SubjectCode { get; set; } = "";
    public string SubjectTitle { get; set; } = "";
    public decimal? Prelim { get; set; }
    public decimal? Midterm { get; set; }
    public decimal? Finals { get; set; }
    public string FinalGrade { get; set; } = ""; // e.g. "1.25", "2.00", "5.00", "INC"
    public string Remarks { get; set; } = ""; // Passed, Failed, Incomplete
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public class StudentAssignmentItem
{
    public int Id { get; set; }
    public int ClassOfferingId { get; set; }
    public int StudentId { get; set; }
    public string SubjectCode { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int MaxScore { get; set; } = 100;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsSubmitted { get; set; } = false;
    public DateTime? SubmittedAt { get; set; }
    public int? MyScore { get; set; }
}

public class StudentFeedbackItem
{
    public int Id { get; set; }
    public int ClassOfferingId { get; set; }
    public int StudentId { get; set; }
    public string FacultyName { get; set; } = "";
    public string AssignmentTitle { get; set; } = "";
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TranscriptRequestRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string DocumentType { get; set; } = "Official Transcript of Records (TOR)";
    public string Purpose { get; set; } = "Employment";
    public int Copies { get; set; } = 1;
    public decimal FeeAmount { get; set; } = 150m;
    public bool IsPaid { get; set; } = false;
    public string Status { get; set; } = "pending"; // pending, processing, ready_for_pickup, released, rejected
    public string QrCodeToken { get; set; } = "";
    public string Remarks { get; set; } = "";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAt { get; set; }
}

public class AddDropRequestRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string RequestType { get; set; } = "add"; // add, drop, change
    public int? CurrentOfferingId { get; set; }
    public int? TargetOfferingId { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedBy { get; set; }
}

public class OverloadRequestRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string RequestType { get; set; } = "overload"; // overload, waiver, cross_enrollment
    public int RequestedUnits { get; set; } = 24;
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}

public class GuidanceReferralRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string OriginatingDept { get; set; } = "Faculty"; // Faculty, Registrar
    public string ReferredBy { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public string ReferralReason { get; set; } = ""; // Failing Grade (5.00), Excessive Absences, Behavioral
    public string Notes { get; set; } = "";
    public string Status { get; set; } = "pending"; // pending, acknowledged, in_progress, resolved
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
