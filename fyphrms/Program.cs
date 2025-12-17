using fyphrms.Data;
using fyphrms.Models;
using fyphrms.Services;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using OfficeOpenXml;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<PayrollCalculatorService>();

var connectionString = builder.Configuration.GetConnectionString("SupabaseConnection")
    ?? throw new InvalidOperationException("Connection string 'SupabaseConnection' not found.");

var supabaseStorageUrl = builder.Configuration["Supabase:StorageUrl"];

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddHttpClient();

builder.Services.AddSingleton(new SupabaseConfig
{
    StorageUrl = supabaseStorageUrl,
    ServiceKey = builder.Configuration["Supabase:ServiceKey"]
});


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});


builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Logging.AddConsole();
builder.Services.AddHttpClient();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var users = userManager.Users.ToList();
    Console.WriteLine("Users in database:");
    foreach (var u in users)
        Console.WriteLine($"Email: {u.Email}, Username: {u.UserName}");

    var adminEmail = "admin@hrms.com";
    var adminPass = "Admin@123";

    if (!users.Any(u => u.Email == adminEmail))
    {
        var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail};
        var result = await userManager.CreateAsync(admin, adminPass);
        if (result.Succeeded)
            Console.WriteLine($"✅ Admin created: {adminEmail} / {adminPass}");
        else
            Console.WriteLine($"⚠️ Failed to create admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();