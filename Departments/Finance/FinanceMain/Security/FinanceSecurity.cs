namespace FinanceMain.Security;

public static class FinanceRoles
{
    public const string FinanceOfficer = "FinanceOfficer";
    public const string Student = "Student";
    public const string Faculty = "Faculty";
}

public sealed class CurrentUserContext
{
    private static readonly Guid DemoStudentId = Guid.Parse("8c2f7bc9-31f2-4c36-9a4f-3f61d5da8b12");

    public Guid GetCurrentStudentId() => DemoStudentId;
}
