using CeramicERP.Models;
using CeramicERP.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(); builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=ceramic.db"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
    var userRole = context.Roles.FirstOrDefault(r => r.Name == "User");

    if (adminRole == null)
    {
        adminRole = new Role { Name = "Admin" };
        context.Roles.Add(adminRole);
    }

    if (userRole == null)
    {
        userRole = new Role { Name = "User" };
        context.Roles.Add(userRole);
    }

    if (context.ChangeTracker.HasChanges())
    {
        context.SaveChanges();
    }

    if (!context.Users.Any(u => u.Email == "admin@erp.com"))
    {
        var adminUser = new User
        {
            Name = "Admin",
            Email = "admin@erp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            RoleId = adminRole.Id,
            IsActive = true
        };

        context.Users.Add(adminUser);
        context.SaveChanges();
    }

    if (!context.Users.Any(u => u.Email == "user@erp.com"))
    {
        var appUser = new User
        {
            Name = "ERP User",
            Email = "user@erp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
            RoleId = userRole.Id,
            IsActive = true
        };

        context.Users.Add(appUser);
        context.SaveChanges();
    }
}

app.Run();
