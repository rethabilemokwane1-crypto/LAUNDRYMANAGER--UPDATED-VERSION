using Microsoft.EntityFrameworkCore;
using LaundryManager.Data;
using LaundryManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PushNotificationService>();
builder.Services.AddHostedService<BookingReminderService>();

var app = builder.Build();

// =========================================================
// CREATE DEMO RESIDENCE
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    var connection = db.Database.GetDbConnection();

    Console.WriteLine("========================================");
    Console.WriteLine("LAUNDRY DATABASE INFORMATION");
    Console.WriteLine($"Server:   {connection.DataSource}");
    Console.WriteLine($"Database: {connection.Database}");
    Console.WriteLine("========================================");

    if (!db.Residences.Any(r => r.Name == "Brandon Mansions"))
    {
        db.Residences.Add(
            new LaundryManager.Models.Residence
            {
                Name = "Brandon Mansions",
                Address = "Demo address — update before launch"
            });

        db.SaveChanges();

        Console.WriteLine("Brandon Mansions residence created.");
    }
}

// =========================================================
// ONE-TIME BRANDON MANSIONS ADMIN CREATION
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    var residence = db.Residences
        .FirstOrDefault(r => r.Name == "Brandon Mansions");

    if (residence != null)
    {
        const string adminEmail =
            "rethabilemokwane1@gmail.com";

        const string adminPassword =
            "Admin$12";

        var existingAdmin = db.Users
            .FirstOrDefault(
                u => u.Email.ToLower() ==
                     adminEmail.ToLower());

        if (existingAdmin == null)
        {
            var admin = new LaundryManager.Models.User
            {
                Email = adminEmail,

                Password =
                    BCrypt.Net.BCrypt.HashPassword(
                        adminPassword),

                IsEmailVerified = true,

                EmailVerificationToken = null,

                PasswordResetToken = null,

                PasswordResetTokenExpiry = null,

                Role = "ResAdmin",

                ResidenceId = residence.Id
            };

            db.Users.Add(admin);
            db.SaveChanges();

            Console.WriteLine("========================================");
            Console.WriteLine("BRANDON MANSIONS ADMIN CREATED");
            Console.WriteLine($"Email:     {adminEmail}");
            Console.WriteLine("Password:  Admin$12");
            Console.WriteLine("Role:      ResAdmin");
            Console.WriteLine($"Residence: {residence.Name}");
            Console.WriteLine("========================================");
        }
        else
        {
            Console.WriteLine("========================================");
            Console.WriteLine("ADMIN ALREADY EXISTS");
            Console.WriteLine($"Email: {existingAdmin.Email}");
            Console.WriteLine($"Role:  {existingAdmin.Role}");
            Console.WriteLine("========================================");
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}");

app.MapControllerRoute(
    name: "indexFallback",
    pattern: "{controller}/{action=Index}/{id?}");

app.Run(); ;
