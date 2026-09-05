using FluentValidation;
using RegistrarMain.HealthAndRepair;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using RegistrarMain.Contracts;
using RegistrarMain.Data;
using RegistrarMain.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Guidance services
builder.Services.AddControllers();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication("DevelopmentTest")
        .AddScheme<AuthenticationSchemeOptions, DevelopmentTestAuthenticationHandler>("DevelopmentTest", _ => { });
}
builder.Services.AddAuthorization();
builder.Services.AddDbContext<RegistrarDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CampusSystemDb"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Registrar", "registrar")));
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
builder.Services.AddRazorPages();

var app = builder.Build();

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

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();


