using CampusSystem.Sql;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var campusConnection = CampusSystemDbConnector.Resolve(
    builder.Configuration.GetConnectionString(CampusSystemDbConnector.ConnectionStringName));

builder.Services.AddDbContextFactory<DbContext>(options =>
    options.UseSqlServer(campusConnection));

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
