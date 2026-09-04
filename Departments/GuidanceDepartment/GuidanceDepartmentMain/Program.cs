using AspNetCoreRateLimit;
using FluentValidation;
using GuidanceDepartmentMain.Contracts;
using GuidanceDepartmentMain.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var signingKey = builder.Configuration["Jwt:SigningKey"];
    if (!string.IsNullOrWhiteSpace(signingKey)) options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true };
    options.Events = new JwtBearerEvents { OnMessageReceived = context => { context.Token = context.Request.Cookies["access_token"]; return Task.CompletedTask; } };
});
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
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseHsts();
app.UseSecurityHeaders();
app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
