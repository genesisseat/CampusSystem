using CampusSystem.Sql;
using FluentValidation;
using RegistrarMain.HealthAndRepair;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using RegistrarMain.Contracts;
using RegistrarMain.Data;
using RegistrarMain.Services;

var builder = WebApplication.CreateBuilder(args);

var campusConnection = CampusSystemDbConnector.Resolve(
    builder.Configuration.GetConnectionString(CampusSystemDbConnector.ConnectionStringName));

builder.Services.AddDbContext<RegistrarDbContext>(options =>
    options.UseSqlServer(campusConnection,
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Registrar", "registrar")));

// Add services to the container.
// Guidance services
builder.Services.AddControllers();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication("DevelopmentTest")
        .AddScheme<AuthenticationSchemeOptions, DevelopmentTestAuthenticationHandler>("DevelopmentTest", _ => { });
}
builder.Services.AddAuthorization();
builder.Services.AddValidatorsFromAssemblyContaining<StudentRequestValidator>();
builder.Services.AddDbContextFactory<GuidanceDbContext>(options =>
    options.UseSqlServer(campusConnection));
builder.Services.AddScoped<IGuidanceRequestStore, SqlGuidanceRequestStore>();
builder.Services.AddScoped<IRefreshTokenStore, SqlRefreshTokenStore>();
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentRequestService, StudentRequestService>();
builder.Services.AddScoped<ICounselorTriageService, CounselorTriageService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddSingleton<IPiiMaskingService, PiiMaskingService>();
builder.Services.AddScoped<IOutboundMessageTransport, UnavailableOutboundMessageTransport>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Registrar Portal Services & MySQL Connection
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddTransient<MySqlConnector.MySqlConnection>(_ =>
    new MySqlConnector.MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

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
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Dashboard");
    return Task.CompletedTask;
});

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages();

app.Run();



