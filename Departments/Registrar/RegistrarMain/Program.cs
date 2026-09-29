using FluentValidation;
using RegistrarMain.HealthAndRepair;
using Microsoft.AspNetCore.Authentication;
using RegistrarMain.Contracts;
using RegistrarMain.Services;

var builder = WebApplication.CreateBuilder(args);

var mysqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

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

// Registrar Portal Services & MySQL Connection
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddSingleton<CampusSystem.Data.Services.CampusJsonDb>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<RegistrarApiSchemaService>();
builder.Services.AddTransient<MySqlConnector.MySqlConnection>(_ =>
<<<<<<< Updated upstream
    new MySqlConnector.MySqlConnection(mysqlConnectionString));
=======
    new MySqlConnector.MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=localhost;Database=dummy"));
>>>>>>> Stashed changes

builder.Services.AddRazorPages();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<RegistrarApiSchemaService>().EnsureSchemaAsync();
}

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



