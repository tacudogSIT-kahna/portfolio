using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
// using portfolio.Data;                      // Task 9: remove the //

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Database: read the connection string from User Secrets and create one shared data source.
var connectionString = builder.Configuration.GetConnectionString("Portfolio");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Portfolio' is missing. Add it in Manage User Secrets.");
}
var dataSource = NpgsqlDataSource.Create(connectionString);
builder.Services.AddSingleton(dataSource);

// Repositories: one class per table, one new object per web request.
// builder.Services.AddScoped<CourseRepository>();        // Task 9: remove the //
// builder.Services.AddScoped<ProjectImageRepository>(); // Task 15: remove the //
// builder.Services.AddScoped<ProjectRepository>();      // Task 15: remove the //

// Sign-in for the editor side. The password itself is in User Secrets, not here.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

var app = builder.Build();

// A friendly page for anything unexpected (500) and for 404, 403 and the rest.
app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithReExecute("/Error/Status/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();   // who is this visitor?
app.UseAuthorization();    // may they open this page?

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Portfolio}/{action=Index}/{id?}");

app.Run();