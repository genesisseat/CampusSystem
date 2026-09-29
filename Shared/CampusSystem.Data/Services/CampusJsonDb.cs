using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using CampusSystem.Data.Models;

namespace CampusSystem.Data.Services;

public class CampusJsonDb
{
    private static readonly object _fileLock = new();
    private readonly string _dataDir;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public CampusJsonDb(string? customDataDir = null)
    {
        if (!string.IsNullOrWhiteSpace(customDataDir) && Directory.Exists(customDataDir))
        {
            _dataDir = customDataDir;
        }
        else
        {
            _dataDir = ResolveDataDirectory();
        }

        Directory.CreateDirectory(_dataDir);
        EnsureSeedData();
    }

    public string DataDirectory => _dataDir;

    private static string ResolveDataDirectory()
    {
        // Search upwards for "Shared/Data" or "CampusSystem/Shared/Data"
        var current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (string.IsNullOrEmpty(current)) break;

            if (Directory.Exists(Path.Combine(current, "Departments")) && Directory.Exists(Path.Combine(current, "Shared")))
            {
                var sharedData = Path.Combine(current, "Shared", "Data");
                Directory.CreateDirectory(sharedData);
                return sharedData;
            }

            var checkPath = Path.Combine(current, "Shared", "Data");
            if (Directory.Exists(checkPath)) return checkPath;

            var parent = Directory.GetParent(current)?.FullName;
            if (parent == null || parent == current) break;
            current = parent;
        }

        // Fallback default relative path from repo root
        var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Shared", "Data"));
        if (!Directory.Exists(fallback))
        {
            Directory.CreateDirectory(fallback);
        }
        return fallback;
    }

    private string GetFilePath(string fileName) => Path.Combine(_dataDir, fileName);

    public List<T> ReadList<T>(string fileName)
    {
        lock (_fileLock)
        {
            var path = GetFilePath(fileName);
            if (!File.Exists(path)) return new List<T>();
            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return new List<T>();
                return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }
    }

    public void WriteList<T>(string fileName, List<T> items)
    {
        lock (_fileLock)
        {
            var path = GetFilePath(fileName);
            var json = JsonSerializer.Serialize(items, _jsonOptions);
            File.WriteAllText(path, json);
        }
    }

    // ==========================================
    // Entity Accessors
    // ==========================================

    public List<CampusUser> GetUsers() => ReadList<CampusUser>("users.json");
    public void SaveUsers(List<CampusUser> items) => WriteList("users.json", items);

    public List<StudentProfileRecord> GetStudentProfiles() => ReadList<StudentProfileRecord>("student_profiles.json");
    public void SaveStudentProfiles(List<StudentProfileRecord> items) => WriteList("student_profiles.json", items);

    public List<SubjectRecord> GetSubjects() => ReadList<SubjectRecord>("subjects.json");
    public void SaveSubjects(List<SubjectRecord> items) => WriteList("subjects.json", items);

    public List<ClassOfferingRecord> GetClassOfferings() => ReadList<ClassOfferingRecord>("class_offerings.json");
    public void SaveClassOfferings(List<ClassOfferingRecord> items) => WriteList("class_offerings.json", items);

    public List<EnrollmentRecord> GetEnrollments() => ReadList<EnrollmentRecord>("enrollments.json");
    public void SaveEnrollments(List<EnrollmentRecord> items) => WriteList("enrollments.json", items);

    public List<EnrolledSubjectRecord> GetEnrolledSubjects() => ReadList<EnrolledSubjectRecord>("enrolled_subjects.json");
    public void SaveEnrolledSubjects(List<EnrolledSubjectRecord> items) => WriteList("enrolled_subjects.json", items);

    public List<FeeAssessmentRecord> GetFeeAssessments() => ReadList<FeeAssessmentRecord>("fee_assessments.json");
    public void SaveFeeAssessments(List<FeeAssessmentRecord> items) => WriteList("fee_assessments.json", items);

    public List<StudentPaymentRecord> GetStudentPayments() => ReadList<StudentPaymentRecord>("student_payments.json");
    public void SaveStudentPayments(List<StudentPaymentRecord> items) => WriteList("student_payments.json", items);

    public List<StudentClearanceRecord> GetStudentClearances() => ReadList<StudentClearanceRecord>("student_clearance.json");
    public void SaveStudentClearances(List<StudentClearanceRecord> items) => WriteList("student_clearance.json", items);

    public List<AttendanceRecordItem> GetAttendanceRecords() => ReadList<AttendanceRecordItem>("attendance_records.json");
    public void SaveAttendanceRecords(List<AttendanceRecordItem> items) => WriteList("attendance_records.json", items);

    public List<GradeRecord> GetGrades() => ReadList<GradeRecord>("grades.json");
    public void SaveGrades(List<GradeRecord> items) => WriteList("grades.json", items);

    public List<StudentAssignmentItem> GetStudentAssignments() => ReadList<StudentAssignmentItem>("student_assignments.json");
    public void SaveStudentAssignments(List<StudentAssignmentItem> items) => WriteList("student_assignments.json", items);

    public List<StudentFeedbackItem> GetStudentFeedback() => ReadList<StudentFeedbackItem>("student_feedback.json");
    public void SaveStudentFeedback(List<StudentFeedbackItem> items) => WriteList("student_feedback.json", items);

    public List<TranscriptRequestRecord> GetTranscriptRequests() => ReadList<TranscriptRequestRecord>("transcript_requests.json");
    public void SaveTranscriptRequests(List<TranscriptRequestRecord> items) => WriteList("transcript_requests.json", items);

    public List<AddDropRequestRecord> GetAddDropRequests() => ReadList<AddDropRequestRecord>("add_drop_requests.json");
    public void SaveAddDropRequests(List<AddDropRequestRecord> items) => WriteList("add_drop_requests.json", items);

    public List<OverloadRequestRecord> GetOverloadRequests() => ReadList<OverloadRequestRecord>("overload_requests.json");
    public void SaveOverloadRequests(List<OverloadRequestRecord> items) => WriteList("overload_requests.json", items);

    public List<GuidanceReferralRecord> GetGuidanceReferrals() => ReadList<GuidanceReferralRecord>("guidance_referrals.json");
    public void SaveGuidanceReferrals(List<GuidanceReferralRecord> items) => WriteList("guidance_referrals.json", items);

    // ==========================================
    // Seed Data Initialization
    // ==========================================

    private void EnsureSeedData()
    {
        lock (_fileLock)
        {
            EnsureUsers();
            EnsureStudentProfiles();
            EnsureSubjects();
            EnsureClassOfferings();
            EnsureEnrollments();
            EnsureFeeAssessments();
            EnsureClearance();
            EnsureTranscriptRequests();
        }
    }

    private void EnsureUsers()
    {
        var path = GetFilePath("users.json");
        if (File.Exists(path) && new FileInfo(path).Length > 10) return;

        var users = new List<CampusUser>
        {
            new() { Id = 1, StudentIdNumber = "EMP-REG-001", Name = "Dr. Rosalinda Santos", Email = "rosalinda.santos@campus.edu", Role = "registrar", Status = "active" },
            new() { Id = 2, StudentIdNumber = "EMP-FAC-001", Name = "Prof. Alan Turing", Email = "alan.turing@campus.edu", Role = "faculty", Status = "active" },
            new() { Id = 3, StudentIdNumber = "EMP-FAC-002", Name = "Engr. Ada Lovelace", Email = "ada.lovelace@campus.edu", Role = "faculty", Status = "active" },
            new() { Id = 4, StudentIdNumber = "EMP-FIN-001", Name = "Roberto Cruz", Email = "roberto.cruz@campus.edu", Role = "finance", Status = "active" },
            new() { Id = 5, StudentIdNumber = "2024-00101", Name = "Alyssa Bea Mendoza", Email = "alyssa.mendoza@student.campus.edu", Role = "student", Status = "active" },
            new() { Id = 6, StudentIdNumber = "2024-00102", Name = "John Daniel Reyes", Email = "john.reyes@student.campus.edu", Role = "student", Status = "active" },
            new() { Id = 7, StudentIdNumber = "2024-00103", Name = "Maria Clara Santos", Email = "maria.santos@student.campus.edu", Role = "student", Status = "active" },
            new() { Id = 8, StudentIdNumber = "2024-00104", Name = "Renzo Aguilar", Email = "renzo.aguilar@student.campus.edu", Role = "student", Status = "active" },
            new() { Id = 9, StudentIdNumber = "EMP-GDC-001", Name = "Dr. Patricia Gomez", Email = "patricia.gomez@campus.edu", Role = "guidance", Status = "active" }
        };
        WriteList("users.json", users);
    }

    private void EnsureStudentProfiles()
    {
        var path = GetFilePath("student_profiles.json");
        if (File.Exists(path) && new FileInfo(path).Length > 10) return;

        var profiles = new List<StudentProfileRecord>
        {
            new() { Id = 1, StudentId = 5, Program = "BS Information Technology", YearLevel = 2, AcademicStatus = "Regular", Gwa = 1.45m, ContactNumber = "0917-555-0101", Address = "Sampaloc, Manila" },
            new() { Id = 2, StudentId = 6, Program = "BS Computer Science", YearLevel = 2, AcademicStatus = "Regular", Gwa = 1.70m, ContactNumber = "0917-555-0102", Address = "Quezon City" },
            new() { Id = 3, StudentId = 7, Program = "BS Information Technology", YearLevel = 3, AcademicStatus = "Regular", Gwa = 1.25m, ContactNumber = "0917-555-0103", Address = "Makati City" },
            new() { Id = 4, StudentId = 8, Program = "BS Business Administration", YearLevel = 1, AcademicStatus = "Regular", Gwa = 1.90m, ContactNumber = "0917-555-0104", Address = "Pasig City" }
        };
        WriteList("student_profiles.json", profiles);
    }

    private void EnsureSubjects()
    {
        var path = GetFilePath("subjects.json");
        if (File.Exists(path) && new FileInfo(path).Length > 10) return;

        var subjects = new List<SubjectRecord>
        {
            new() { Id = 1, Code = "CC101", Title = "Introduction to Computing", Units = 3, LectureHours = 3, LabHours = 0, Prerequisite = "None" },
            new() { Id = 2, Code = "CC102", Title = "Data Structures and Algorithms", Units = 3, LectureHours = 2, LabHours = 3, Prerequisite = "CC101" },
            new() { Id = 3, Code = "IT201", Title = "Web Systems and Technologies", Units = 3, LectureHours = 2, LabHours = 3, Prerequisite = "CC102" },
            new() { Id = 4, Code = "IT202", Title = "Database Management Systems", Units = 3, LectureHours = 2, LabHours = 3, Prerequisite = "CC101" },
            new() { Id = 5, Code = "CS301", Title = "Operating Systems Architecture", Units = 3, LectureHours = 3, LabHours = 0, Prerequisite = "CC102" },
            new() { Id = 6, Code = "MATH101", Title = "Calculus for Computing", Units = 3, LectureHours = 3, LabHours = 0, Prerequisite = "None" },
            new() { Id = 7, Code = "ENG101", Title = "Purposive Communication", Units = 3, LectureHours = 3, LabHours = 0, Prerequisite = "None" }
        };
        WriteList("subjects.json", subjects);
    }

    private void EnsureClassOfferings()
    {
        var path = GetFilePath("class_offerings.json");
        if (File.Exists(path) && new FileInfo(path).Length > 10) return;

        var offerings = new List<ClassOfferingRecord>
        {
            new() { Id = 1, SectionCode = "BSIT-2A", SubjectId = 1, SubjectCode = "CC101", SubjectTitle = "Introduction to Computing", Units = 3, Room = "LEC-301", DaysOfWeek = "MWF", StartTime = "08:00 AM", EndTime = "09:00 AM", InstructorName = "Prof. Alan Turing", MaxSlots = 40, SlotsTaken = 25 },
            new() { Id = 2, SectionCode = "BSIT-2A", SubjectId = 2, SubjectCode = "CC102", SubjectTitle = "Data Structures and Algorithms", Units = 3, Room = "CL-204", DaysOfWeek = "TTH", StartTime = "09:00 AM", EndTime = "10:30 AM", InstructorName = "Prof. Alan Turing", MaxSlots = 35, SlotsTaken = 28 },
            new() { Id = 3, SectionCode = "BSIT-2A", SubjectId = 3, SubjectCode = "IT201", SubjectTitle = "Web Systems and Technologies", Units = 3, Room = "CL-301", DaysOfWeek = "MWF", StartTime = "10:00 AM", EndTime = "11:30 AM", InstructorName = "Engr. Ada Lovelace", MaxSlots = 35, SlotsTaken = 30 },
            new() { Id = 4, SectionCode = "BSIT-2A", SubjectId = 4, SubjectCode = "IT202", SubjectTitle = "Database Management Systems", Units = 3, Room = "CL-302", DaysOfWeek = "TTH", StartTime = "01:00 PM", EndTime = "02:30 PM", InstructorName = "Engr. Ada Lovelace", MaxSlots = 35, SlotsTaken = 22 },
            new() { Id = 5, SectionCode = "BSCS-2A", SubjectId = 5, SubjectCode = "CS301", SubjectTitle = "Operating Systems Architecture", Units = 3, Room = "LEC-401", DaysOfWeek = "MWF", StartTime = "01:00 PM", EndTime = "02:00 PM", InstructorName = "Prof. Alan Turing", MaxSlots = 40, SlotsTaken = 18 },
            new() { Id = 6, SectionCode = "BSIT-2A", SubjectId = 6, SubjectCode = "MATH101", SubjectTitle = "Calculus for Computing", Units = 3, Room = "LEC-205", DaysOfWeek = "MWF", StartTime = "02:00 PM", EndTime = "03:00 PM", InstructorName = "Prof. Isaac Newton", MaxSlots = 45, SlotsTaken = 35 },
            new() { Id = 7, SectionCode = "BSIT-2A", SubjectId = 7, SubjectCode = "ENG101", SubjectTitle = "Purposive Communication", Units = 3, Room = "LEC-102", DaysOfWeek = "TTH", StartTime = "03:00 PM", EndTime = "04:30 PM", InstructorName = "Prof. William Shakespeare", MaxSlots = 45, SlotsTaken = 40 }
        };
        WriteList("class_offerings.json", offerings);
    }

    private void EnsureEnrollments()
    {
        var path = GetFilePath("enrollments.json");
        if (File.Exists(path) && new FileInfo(path).Length > 10) return;

        // Seed 1 active/enrolled student (Maria Clara) and 1 pending reservation for Alyssa Bea Mendoza so Registrar has work to approve!
        var enrollments = new List<EnrollmentRecord>
        {
            new()
            {
                Id = 1,
                StudentId = 7, // Maria Clara
                StudentNumber = "2024-00103",
                StudentName = "Maria Clara Santos",
                Program = "BS Information Technology",
                SchoolYear = "2026-2027",
                Semester = "1st Semester",
                EnrollmentType = "Regular",
                Status = "enrolled",
                TotalUnits = 21,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ApprovedAt = DateTime.UtcNow.AddDays(-9),
                ApprovedBy = "Dr. Rosalinda Santos"
            },
            new()
            {
                Id = 2,
                StudentId = 5, // Alyssa Bea Mendoza
                StudentNumber = "2024-00101",
                StudentName = "Alyssa Bea Mendoza",
                Program = "BS Information Technology",
                SchoolYear = "2026-2027",
                Semester = "1st Semester",
                EnrollmentType = "Regular",
                Status = "pending",
                TotalUnits = 21,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            }
        };
        WriteList("enrollments.json", enrollments);

        // Seed enrolled subjects for both
        var enrolledSubjects = new List<EnrolledSubjectRecord>
        {
            // Maria Clara (Enrolled)
            new() { Id = 1, EnrollmentId = 1, StudentId = 7, ClassOfferingId = 1, SubjectCode = "CC101", SubjectTitle = "Introduction to Computing", Units = 3, Status = "enrolled" },
            new() { Id = 2, EnrollmentId = 1, StudentId = 7, ClassOfferingId = 2, SubjectCode = "CC102", SubjectTitle = "Data Structures and Algorithms", Units = 3, Status = "enrolled" },
            new() { Id = 3, EnrollmentId = 1, StudentId = 7, ClassOfferingId = 3, SubjectCode = "IT201", SubjectTitle = "Web Systems and Technologies", Units = 3, Status = "enrolled" },
            new() { Id = 4, EnrollmentId = 1, StudentId = 7, ClassOfferingId = 4, SubjectCode = "IT202", SubjectTitle = "Database Management Systems", Units = 3, Status = "enrolled" },
            new() { Id = 5, EnrollmentId = 1, StudentId = 7, ClassOfferingId = 5, SubjectCode = "CS301", SubjectTitle = "Operating Systems Architecture", Units = 3, Status = "enrolled" },
            new() { Id = 6, EnrollmentId = 1, StudentId = 7, ClassOfferingId = 6, SubjectCode = "MATH101", SubjectTitle = "Calculus for Computing", Units = 3, Status = "enrolled" },
            new() { Id = 7, EnrollmentId = 1, StudentId = 7, ClassOfferingId = 7, SubjectCode = "ENG101", SubjectTitle = "Purposive Communication", Units = 3, Status = "enrolled" },

            // Alyssa Bea Mendoza (Pending enrollment selection)
            new() { Id = 8, EnrollmentId = 2, StudentId = 5, ClassOfferingId = 1, SubjectCode = "CC101", SubjectTitle = "Introduction to Computing", Units = 3, Status = "enrolled" },
            new() { Id = 9, EnrollmentId = 2, StudentId = 5, ClassOfferingId = 2, SubjectCode = "CC102", SubjectTitle = "Data Structures and Algorithms", Units = 3, Status = "enrolled" },
            new() { Id = 10, EnrollmentId = 2, StudentId = 5, ClassOfferingId = 3, SubjectCode = "IT201", SubjectTitle = "Web Systems and Technologies", Units = 3, Status = "enrolled" },
            new() { Id = 11, EnrollmentId = 2, StudentId = 5, ClassOfferingId = 4, SubjectCode = "IT202", SubjectTitle = "Database Management Systems", Units = 3, Status = "enrolled" },
            new() { Id = 12, EnrollmentId = 2, StudentId = 5, ClassOfferingId = 5, SubjectCode = "CS301", SubjectTitle = "Operating Systems Architecture", Units = 3, Status = "enrolled" },
            new() { Id = 13, EnrollmentId = 2, StudentId = 5, ClassOfferingId = 6, SubjectCode = "MATH101", SubjectTitle = "Calculus for Computing", Units = 3, Status = "enrolled" },
            new() { Id = 14, EnrollmentId = 2, StudentId = 5, ClassOfferingId = 7, SubjectCode = "ENG101", SubjectTitle = "Purposive Communication", Units = 3, Status = "enrolled" }
        };
        WriteList("enrolled_subjects.json", enrolledSubjects);
    }

    private void EnsureFeeAssessments()
    {
        var path = GetFilePath("fee_assessments.json");
        if (File.Exists(path) && new FileInfo(path).Length > 10) return;

        var assessments = new List<FeeAssessmentRecord>
        {
            // Maria Clara - Fully Paid
            new()
            {
                Id = 1,
                AssessmentNumber = "ASM-2026-00007",
                StudentId = 7,
                StudentNumber = "2024-00103",
                StudentName = "Maria Clara Santos",
                Program = "BS Information Technology",
                SchoolYear = "2026-2027",
                Semester = "1st Semester",
                TotalUnits = 21,
                TuitionFee = 31500m,
                MiscellaneousFee = 4500m,
                TotalAmount = 36000m,
                TotalPaid = 36000m,
                Status = "Fully Paid",
                CreatedAt = DateTime.UtcNow.AddDays(-9)
            }
        };
        WriteList("fee_assessments.json", assessments);

        // Payment record for Maria Clara
        var payments = new List<StudentPaymentRecord>
        {
            new()
            {
                Id = 1,
                ReceiptNo = "OR-2026-00101",
                FeeAssessmentId = 1,
                StudentId = 7,
                StudentNumber = "2024-00103",
                StudentName = "Maria Clara Santos",
                Amount = 36000m,
                PaymentMethod = "Online Banking",
                ReferenceNumber = "PAY-REF-98124",
                PayDate = DateTime.UtcNow.AddDays(-8),
                SchoolYear = "2026-2027",
                Semester = "1st Semester"
            }
        };
        WriteList("student_payments.json", payments);
    }

    private void EnsureClearance()
    {
        var path = GetFilePath("student_clearance.json");
        if (File.Exists(path) && new FileInfo(path).Length > 10) return;

        var clearances = new List<StudentClearanceRecord>
        {
            // Maria Clara - Cleared all
            new() { Id = 1, StudentId = 7, StudentNumber = "2024-00103", StudentName = "Maria Clara Santos", DepartmentName = "Finance", Status = "Cleared", Remarks = "Tuition settled" },
            new() { Id = 2, StudentId = 7, StudentNumber = "2024-00103", StudentName = "Maria Clara Santos", DepartmentName = "Registrar", Status = "Cleared", Remarks = "Credentials verified" },
            new() { Id = 3, StudentId = 7, StudentNumber = "2024-00103", StudentName = "Maria Clara Santos", DepartmentName = "Library", Status = "Cleared", Remarks = "No unreturned books" },
            new() { Id = 4, StudentId = 7, StudentNumber = "2024-00103", StudentName = "Maria Clara Santos", DepartmentName = "Guidance", Status = "Cleared", Remarks = "Good standing" },

            // Alyssa Bea Mendoza - Pending
            new() { Id = 5, StudentId = 5, StudentNumber = "2024-00101", StudentName = "Alyssa Bea Mendoza", DepartmentName = "Finance", Status = "Pending", Remarks = "Awaiting tuition settlement" },
            new() { Id = 6, StudentId = 5, StudentNumber = "2024-00101", StudentName = "Alyssa Bea Mendoza", DepartmentName = "Registrar", Status = "Cleared", Remarks = "Credentials verified" },
            new() { Id = 7, StudentId = 5, StudentNumber = "2024-00101", StudentName = "Alyssa Bea Mendoza", DepartmentName = "Library", Status = "Cleared", Remarks = "Clear" },
            new() { Id = 8, StudentId = 5, StudentNumber = "2024-00101", StudentName = "Alyssa Bea Mendoza", DepartmentName = "Guidance", Status = "Cleared", Remarks = "Clear" }
        };
        WriteList("student_clearance.json", clearances);
    }

    private void EnsureTranscriptRequests()
    {
        var path = GetFilePath("transcript_requests.json");
        if (File.Exists(path) && new FileInfo(path).Length > 10) return;

        var requests = new List<TranscriptRequestRecord>
        {
            new()
            {
                Id = 1,
                StudentId = 7,
                StudentNumber = "2024-00103",
                StudentName = "Maria Clara Santos",
                DocumentType = "Official Transcript of Records (TOR)",
                Purpose = "Scholarship Application",
                Copies = 2,
                FeeAmount = 300m,
                IsPaid = true,
                Status = "ready_for_pickup",
                QrCodeToken = "MSU-TOR-2026-MC007",
                Remarks = "Printed and verified by Registrar",
                RequestedAt = DateTime.UtcNow.AddDays(-3)
            }
        };
        WriteList("transcript_requests.json", requests);
    }

    // ==========================================
    // High-Level Interconnected Business Actions
    // ==========================================

    /// <summary>
    /// Student submits an enrollment reservation with chosen class offerings.
    /// </summary>
    public EnrollmentRecord SubmitEnrollmentReservation(int studentId, List<int> selectedOfferingIds, string schoolYear = "2026-2027", string semester = "1st Semester")
    {
        var users = GetUsers();
        var user = users.FirstOrDefault(u => u.Id == studentId);
        var profiles = GetStudentProfiles();
        var profile = profiles.FirstOrDefault(p => p.StudentId == studentId);
        var offerings = GetClassOfferings();

        var enrollments = GetEnrollments();
        var existing = enrollments.FirstOrDefault(e => e.StudentId == studentId && e.SchoolYear == schoolYear && e.Semester == semester);

        var selectedOfferings = offerings.Where(o => selectedOfferingIds.Contains(o.Id)).ToList();
        int totalUnits = selectedOfferings.Sum(o => o.Units);

        var enrolledSubjects = GetEnrolledSubjects();

        EnrollmentRecord enrollment;
        if (existing != null)
        {
            existing.Status = "pending";
            existing.TotalUnits = totalUnits;
            existing.CreatedAt = DateTime.UtcNow;
            existing.ApprovedAt = null;
            existing.ApprovedBy = null;
            enrollment = existing;

            // Remove previous subject selections for this enrollment
            enrolledSubjects.RemoveAll(es => es.EnrollmentId == existing.Id);
        }
        else
        {
            int newId = (enrollments.MaxBy(e => e.Id)?.Id ?? 0) + 1;
            enrollment = new EnrollmentRecord
            {
                Id = newId,
                StudentId = studentId,
                StudentNumber = user?.StudentIdNumber ?? $"2024-{studentId:D5}",
                StudentName = user?.Name ?? "Student",
                Program = profile?.Program ?? "BS Information Technology",
                SchoolYear = schoolYear,
                Semester = semester,
                EnrollmentType = "Regular",
                Status = "pending",
                TotalUnits = totalUnits,
                CreatedAt = DateTime.UtcNow
            };
            enrollments.Add(enrollment);
        }

        SaveEnrollments(enrollments);

        // Insert enrolled subjects
        int nextSubId = (enrolledSubjects.MaxBy(es => es.Id)?.Id ?? 0) + 1;
        foreach (var off in selectedOfferings)
        {
            enrolledSubjects.Add(new EnrolledSubjectRecord
            {
                Id = nextSubId++,
                EnrollmentId = enrollment.Id,
                StudentId = studentId,
                ClassOfferingId = off.Id,
                SubjectCode = off.SubjectCode,
                SubjectTitle = off.SubjectTitle,
                Units = off.Units,
                Status = "enrolled"
            });
        }
        SaveEnrolledSubjects(enrolledSubjects);

        return enrollment;
    }

    /// <summary>
    /// Registrar approves pending enrollment -> status becomes 'active' -> creates fee assessment!
    /// </summary>
    public bool ApproveEnrollment(int enrollmentId, string approvedBy = "Registrar Staff")
    {
        var enrollments = GetEnrollments();
        var enrollee = enrollments.FirstOrDefault(e => e.Id == enrollmentId);
        if (enrollee == null) return false;

        enrollee.Status = "active";
        enrollee.ApprovedAt = DateTime.UtcNow;
        enrollee.ApprovedBy = approvedBy;
        SaveEnrollments(enrollments);

        // Auto-generate Fee Assessment in fee_assessments.json
        var assessments = GetFeeAssessments();
        var existingAsm = assessments.FirstOrDefault(a => a.StudentId == enrollee.StudentId && a.SchoolYear == enrollee.SchoolYear && a.Semester == enrollee.Semester);

        decimal tuitionPerUnit = 1500m;
        decimal misc = 4500m;
        decimal totalTuition = enrollee.TotalUnits * tuitionPerUnit;
        decimal totalFee = totalTuition + misc;

        if (existingAsm != null)
        {
            existingAsm.TotalUnits = enrollee.TotalUnits;
            existingAsm.TuitionFee = totalTuition;
            existingAsm.MiscellaneousFee = misc;
            existingAsm.TotalAmount = totalFee;
        }
        else
        {
            int nextAsmId = (assessments.MaxBy(a => a.Id)?.Id ?? 0) + 1;
            assessments.Add(new FeeAssessmentRecord
            {
                Id = nextAsmId,
                AssessmentNumber = $"ASM-2026-{enrollee.StudentId:D5}",
                StudentId = enrollee.StudentId,
                StudentNumber = enrollee.StudentNumber,
                StudentName = enrollee.StudentName,
                Program = enrollee.Program,
                SchoolYear = enrollee.SchoolYear,
                Semester = enrollee.Semester,
                TotalUnits = enrollee.TotalUnits,
                TuitionFee = totalTuition,
                MiscellaneousFee = misc,
                TotalAmount = totalFee,
                TotalPaid = 0m,
                Status = "Unpaid",
                CreatedAt = DateTime.UtcNow
            });
        }
        SaveFeeAssessments(assessments);

        // Update slots taken on class offerings
        var enrolledSubjects = GetEnrolledSubjects().Where(es => es.EnrollmentId == enrollee.Id).ToList();
        var offerings = GetClassOfferings();
        foreach (var sub in enrolledSubjects)
        {
            var off = offerings.FirstOrDefault(o => o.Id == sub.ClassOfferingId);
            if (off != null) off.SlotsTaken = Math.Min(off.MaxSlots, off.SlotsTaken + 1);
        }
        SaveClassOfferings(offerings);

        return true;
    }

    /// <summary>
    /// Student or Cashier submits payment -> updates assessment, clearance, and marks enrollment as 'enrolled'.
    /// </summary>
    public StudentPaymentRecord RecordPayment(int studentId, decimal amount, string paymentMethod = "Online Banking", string referenceNo = "")
    {
        var users = GetUsers();
        var user = users.FirstOrDefault(u => u.Id == studentId);
        var assessments = GetFeeAssessments();
        var assessment = assessments.FirstOrDefault(a => a.StudentId == studentId);

        var payments = GetStudentPayments();
        int nextPayId = (payments.MaxBy(p => p.Id)?.Id ?? 0) + 1;
        var receiptNo = $"OR-2026-{nextPayId:D5}";

        var payment = new StudentPaymentRecord
        {
            Id = nextPayId,
            ReceiptNo = receiptNo,
            FeeAssessmentId = assessment?.Id ?? 0,
            StudentId = studentId,
            StudentNumber = user?.StudentIdNumber ?? $"2024-{studentId:D5}",
            StudentName = user?.Name ?? "Student",
            Amount = amount,
            PaymentMethod = paymentMethod,
            ReferenceNumber = string.IsNullOrWhiteSpace(referenceNo) ? $"REF-{Guid.NewGuid().ToString("N")[..8].ToUpper()}" : referenceNo,
            PayDate = DateTime.UtcNow,
            SchoolYear = assessment?.SchoolYear ?? "2026-2027",
            Semester = assessment?.Semester ?? "1st Semester"
        };
        payments.Add(payment);
        SaveStudentPayments(payments);

        // Update Assessment
        if (assessment != null)
        {
            assessment.TotalPaid += amount;
            if (assessment.TotalPaid >= assessment.TotalAmount)
            {
                assessment.Status = "Fully Paid";
            }
            else if (assessment.TotalPaid > 0)
            {
                assessment.Status = "Partially Paid";
            }
            SaveFeeAssessments(assessments);
        }

        // If downpayment of at least 5000 is paid, activate full enrollment status & clear Finance!
        var totalPaidForStudent = payments.Where(p => p.StudentId == studentId).Sum(p => p.Amount);
        if (totalPaidForStudent >= 5000m)
        {
            var enrollments = GetEnrollments();
            var enr = enrollments.FirstOrDefault(e => e.StudentId == studentId);
            if (enr != null && (enr.Status == "active" || enr.Status == "pending"))
            {
                enr.Status = "enrolled";
                SaveEnrollments(enrollments);
            }

            var clearances = GetStudentClearances();
            var finClearance = clearances.FirstOrDefault(c => c.StudentId == studentId && c.DepartmentName == "Finance");
            if (finClearance != null)
            {
                finClearance.Status = "Cleared";
                finClearance.Remarks = $"Downpayment of ₱{totalPaidForStudent:N2} confirmed.";
                finClearance.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                clearances.Add(new StudentClearanceRecord
                {
                    Id = (clearances.MaxBy(c => c.Id)?.Id ?? 0) + 1,
                    StudentId = studentId,
                    StudentNumber = user?.StudentIdNumber ?? $"2024-{studentId:D5}",
                    StudentName = user?.Name ?? "Student",
                    DepartmentName = "Finance",
                    Status = "Cleared",
                    Remarks = $"Downpayment of ₱{totalPaidForStudent:N2} confirmed.",
                    UpdatedAt = DateTime.UtcNow
                });
            }
            SaveStudentClearances(clearances);
        }

        return payment;
    }

    /// <summary>
    /// Faculty submits grade -> if failing grade (5.00) or INC, auto-triggers Guidance referral!
    /// </summary>
    public void SubmitGrade(int studentId, int classOfferingId, string subjectCode, string subjectTitle, decimal? prelim, decimal? midterm, decimal? finals, string finalGrade, string facultyName = "Faculty Member")
    {
        var users = GetUsers();
        var student = users.FirstOrDefault(u => u.Id == studentId);

        var grades = GetGrades();
        var gradeRec = grades.FirstOrDefault(g => g.StudentId == studentId && g.ClassOfferingId == classOfferingId);

        string remarks = "Passed";
        if (finalGrade == "5.00" || finalGrade.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
        {
            remarks = "Failed";
        }
        else if (finalGrade.Equals("INC", StringComparison.OrdinalIgnoreCase))
        {
            remarks = "Incomplete";
        }

        if (gradeRec != null)
        {
            gradeRec.Prelim = prelim;
            gradeRec.Midterm = midterm;
            gradeRec.Finals = finals;
            gradeRec.FinalGrade = finalGrade;
            gradeRec.Remarks = remarks;
            gradeRec.SubmittedAt = DateTime.UtcNow;
        }
        else
        {
            int nextId = (grades.MaxBy(g => g.Id)?.Id ?? 0) + 1;
            grades.Add(new GradeRecord
            {
                Id = nextId,
                StudentId = studentId,
                ClassOfferingId = classOfferingId,
                SubjectCode = subjectCode,
                SubjectTitle = subjectTitle,
                Prelim = prelim,
                Midterm = midterm,
                Finals = finals,
                FinalGrade = finalGrade,
                Remarks = remarks,
                SubmittedAt = DateTime.UtcNow
            });
        }
        SaveGrades(grades);

        // AUTO-REFERRAL TO GUIDANCE: If student receives 5.00 (Failed) or INC
        if (remarks == "Failed" || remarks == "Incomplete")
        {
            var referrals = GetGuidanceReferrals();
            var existingRef = referrals.FirstOrDefault(r => r.StudentId == studentId && r.SubjectCode == subjectCode && r.Status == "pending");
            if (existingRef == null)
            {
                int nextRefId = (referrals.MaxBy(r => r.Id)?.Id ?? 0) + 1;
                referrals.Add(new GuidanceReferralRecord
                {
                    Id = nextRefId,
                    StudentId = studentId,
                    StudentNumber = student?.StudentIdNumber ?? $"2024-{studentId:D5}",
                    StudentName = student?.Name ?? "Student",
                    OriginatingDept = "Faculty",
                    ReferredBy = facultyName,
                    SubjectCode = subjectCode,
                    ReferralReason = remarks == "Failed" ? "Academic Deficiency: Final Grade 5.00 (Failed)" : "Academic Warning: Incomplete (INC) Grade",
                    Notes = $"Automated referral generated upon faculty grade submission for {subjectCode} ({subjectTitle}).",
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                });
                SaveGuidanceReferrals(referrals);
            }
        }
    }

    /// <summary>
    /// Student requests a document -> stored in transcript_requests.json with QR code token.
    /// </summary>
    public TranscriptRequestRecord SubmitTranscriptRequest(int studentId, string documentType, string purpose, int copies)
    {
        var users = GetUsers();
        var student = users.FirstOrDefault(u => u.Id == studentId);

        decimal feePerCopy = documentType.Contains("TOR", StringComparison.OrdinalIgnoreCase) ? 150m : 50m;
        decimal totalFee = feePerCopy * copies;

        string prefix = "DOC";
        if (documentType.Contains("TOR", StringComparison.OrdinalIgnoreCase)) prefix = "TOR";
        else if (documentType.Contains("Moral", StringComparison.OrdinalIgnoreCase)) prefix = "GMC";
        else if (documentType.Contains("Enrollment", StringComparison.OrdinalIgnoreCase)) prefix = "COE";

        string token = $"MSU-{prefix}-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var requests = GetTranscriptRequests();
        int nextId = (requests.MaxBy(r => r.Id)?.Id ?? 0) + 1;

        var rec = new TranscriptRequestRecord
        {
            Id = nextId,
            StudentId = studentId,
            StudentNumber = student?.StudentIdNumber ?? $"2024-{studentId:D5}",
            StudentName = student?.Name ?? "Student",
            DocumentType = documentType,
            Purpose = purpose,
            Copies = copies,
            FeeAmount = totalFee,
            IsPaid = false,
            Status = "pending",
            QrCodeToken = token,
            Remarks = "Submitted by student via portal",
            RequestedAt = DateTime.UtcNow
        };
        requests.Add(rec);
        SaveTranscriptRequests(requests);

        return rec;
    }

    /// <summary>
    /// Registrar processes or releases a transcript request.
    /// </summary>
    public bool UpdateTranscriptRequestStatus(int requestId, string newStatus, string remarks = "")
    {
        var requests = GetTranscriptRequests();
        var req = requests.FirstOrDefault(r => r.Id == requestId);
        if (req == null) return false;

        req.Status = newStatus;
        if (!string.IsNullOrWhiteSpace(remarks)) req.Remarks = remarks;
        if (newStatus == "released")
        {
            req.ReleasedAt = DateTime.UtcNow;
            req.IsPaid = true;
        }
        SaveTranscriptRequests(requests);
        return true;
    }
}
