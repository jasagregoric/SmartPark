using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartPark.Data;
using SmartPark.Models;

var builder = WebApplication.CreateBuilder(args);

// Connection string
var connectionString = builder.Configuration.GetConnectionString("SmartParkContext");

// Dodaj DbContext
builder.Services.AddDbContext<SmartParkContext>(options =>
    options.UseSqlServer(connectionString));

// Dodaj Identity z vlogami
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<SmartParkContext>();

// Dodaj MVC in Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
    // Vse strani in kontrolerji privzeto zahtevajo prijavo
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
});

// Build aplikacijo
var app = builder.Build();

// Inicializiraj bazo (seed)
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

// Middleware pipeline
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

// Route za kontrolerje
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Route za Razor Pages
app.MapRazorPages();

app.Run();
