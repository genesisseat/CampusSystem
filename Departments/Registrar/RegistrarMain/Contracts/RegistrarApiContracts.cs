namespace RegistrarMain.Contracts;

public record CourseDto(
	int Id,
	string Code,
	string Title,
	decimal Credits,
	int? OfferingId = null,
	string? SectionCode = null,
	string? Semester = null,
	string? SchoolYear = null);
public record CreateCourseRequest(
	string Code,
	string Title,
	int Credits,
	string Program = "General",
	string YearLevel = "1st Year",
	string Semester = "1st Semester",
	int LectureHours = 0,
	int LabHours = 0,
	int? PrerequisiteSubjectId = null);
public record EnrollmentRequestDto(int OfferingId);
public record EnrollmentDto(int Id, int CourseId, string CourseCode, string Semester);
public record TranscriptEntryDto(string CourseCode, string Grade);
public record TranscriptSemesterDto(string Semester, IReadOnlyList<TranscriptEntryDto> Entries);
public record TranscriptDto(IReadOnlyList<TranscriptSemesterDto> Semesters);
public record VerificationRequestDto(string? Purpose = null);
public record VerificationResponseDto(int Id, string Status, DateTime RequestedAt);
public record RecordsRequestDto(string DocumentType);
public record RecordsResponseDto(int Id, string DocumentType, string Status, DateTime RequestedAt);
