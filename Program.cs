using Microsoft.AspNetCore.Authentication.Cookies;
using UserManagement.Data;
using UserManagement.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                          ?? builder.Configuration.GetConnectionString("DefaultConnection");

string connectionString;
if (!string.IsNullOrEmpty(rawConnectionString) && rawConnectionString.StartsWith("postgresql://"))
{
    var uri = new Uri(rawConnectionString);
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var database = uri.AbsolutePath.TrimStart('/');
    var host = uri.Host;
    var port = uri.Port;
    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
}
else
{
    connectionString = rawConnectionString;
}

Console.WriteLine($"Connection string: {connectionString}");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddScoped<IEmailService, SendGridEmailService>();
var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate(); 
}
app.Run();
