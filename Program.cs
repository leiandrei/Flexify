using GymCrudApp.Data;
using GymCrudApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<GymDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configure ASP.NET Core Identity with Roles and Lockout
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        // Lockout settings for repeated failed login attempts
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<GymDbContext>();

// 3. Point Identity's cookie redirects to your custom AccountController
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// register the services
builder.Services.AddScoped<DbInitializer>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IMembershipPlanService, MembershipPlanService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IMemberPortalService, MemberPortalService>();
builder.Services.AddControllersWithViews();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddScoped<DbInitializer>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 5. Run the seed class on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var initializer = services.GetRequiredService<DbInitializer>();
    await initializer.SeedAsync();
}

// 6. Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
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