using CampusSystem.Sql;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StudentPortalMain.Contracts;
using StudentPortalMain.Services;

var builder = WebApplication.CreateBuilder(args);

var campusConnection = CampusSystemDbConnector.Resolve(
    builder.Configuration.GetConnectionString(CampusSystemDbConnector.ConnectionStringName));

builder.Services.AddDbContextFactory<DbContext>(options =>
    options.UseSqlServer(campusConnection));

// Add services to the container.
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

// Student Portal & Registrar Database Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddTransient<MySqlConnector.MySqlConnection>(_ =>
    new MySqlConnector.MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<StudentPortalDbService>();

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

app.UseAuthorization();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Dashboard");
    return Task.CompletedTask;
});

app.MapStaticAssets();
app.MapRazorPages();

app.Run();
