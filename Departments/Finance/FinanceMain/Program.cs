using FluentValidation;
using FinanceMain.Contracts;
using FinanceMain.Security;
using FinanceMain.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Guidance services
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<StudentRequestValidator>();
builder.Services.AddSingleton<IGuidanceRequestStore, InMemoryGuidanceRequestStore>();
builder.Services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentRequestService, StudentRequestService>();
builder.Services.AddScoped<ICounselorTriageService, CounselorTriageService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddSingleton<IPiiMaskingService, PiiMaskingService>();
builder.Services.AddScoped<IOutboundMessageTransport, UnavailableOutboundMessageTransport>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddSingleton<CampusSystem.Data.Services.CampusJsonDb>();
builder.Services.AddSingleton<FinanceDataStore>();
builder.Services.AddSingleton<FinanceDbService>();
builder.Services.AddScoped<CurrentUserContext>();
builder.Services.AddScoped<IAssessmentService, ActiveAssessmentService>();
builder.Services.AddScoped<IPaymentService, ActivePaymentService>();
builder.Services.AddScoped<IClearanceService, ActiveClearanceService>();
builder.Services.AddScoped<IRequestService, ActiveRequestService>();
builder.Services.AddScoped<IClassRosterProvider, RegistrarClassRosterProvider>();

builder.Services.AddRazorPages();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultAuthenticateScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
}).AddCookie("Cookies", options =>
{
    options.LoginPath = "/Dashboard";
    options.AccessDeniedPath = "/Dashboard";
});

builder.Services.AddAuthorization();

var app = builder.Build();

await app.Services.GetRequiredService<FinanceDbService>().EnsureSchemaAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Root → Finance admin executive dashboard.
app.MapGet("/", () => Results.Redirect("/Dashboard"));

// Cross-Department REST API Endpoints for Registrar, StudentPortal, and FacultyPortal
app.MapPost("/api/finance/assess", async (EnrolleeAssessmentDto input, IAssessmentService assessmentService) =>
{
    var guid = Guid.TryParse(input.StudentGuid, out var g) ? g : Guid.NewGuid();
    var req = new EnrolleeAssessmentRequest(
        guid,
        input.StudentNumber ?? "2026-00000",
        input.StudentName ?? "Student",
        input.Program ?? "BS Information Technology",
        input.AcademicYear ?? "2026-2027",
        input.Semester ?? "1st Semester",
        input.TotalUnits > 0 ? input.TotalUnits : 21
    );

    var res = await assessmentService.AssessEnrolleeAsync(req);
    return Results.Ok(new
    {
        succeeded = true,
        isExisting = res.IsExisting,
        message = res.IsExisting
            ? "Student was already assessed for this academic term. Duplicate charge prevented."
            : "Official Assessment Order generated and linked to enrollment.",
        assessment = res.Assessment
    });
});

app.MapGet("/api/finance/assessment", async (string? studentNumber, string? term, IAssessmentService assessmentService) =>
{
    var list = await assessmentService.ListAsync(new AssessmentListFilter(
        term ?? "2026-2027", "1st Semester", null, studentNumber, 1, 5));
    var match = list.Items.FirstOrDefault();
    return match != null ? Results.Ok(new { succeeded = true, assessment = match }) : Results.NotFound(new { succeeded = false, message = "Assessment not found." });
});

app.MapGet("/api/finance/clearance", async (string? studentNumber, IClearanceService clearanceService) =>
{
    var list = await clearanceService.ListAsync(new ClearanceListFilter(
        "2026-2027", "1st Semester", null, studentNumber, 1, 5));
    var match = list.Items.FirstOrDefault();
    return match != null ? Results.Ok(new { succeeded = true, clearance = match }) : Results.NotFound(new { succeeded = false, message = "Clearance not found." });
});

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public record EnrolleeAssessmentDto(string? StudentGuid, string? StudentNumber, string? StudentName, string? Program, string? AcademicYear, string? Semester, int TotalUnits);



