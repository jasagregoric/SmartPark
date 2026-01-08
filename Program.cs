using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartPark.Data;
using SmartPark.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Connection string
var connectionString = builder.Configuration.GetConnectionString("SmartParkContext");
builder.Services.AddDbContext<SmartParkContext>(options => options.UseSqlServer(connectionString));

// Dodaj Identity z vlogami
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<SmartParkContext>();

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
});

// Build aplikacijo
var app = builder.Build();

app.MapControllerRoute( name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// Inicializiraj bazo (seed)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.SeedOverpassParking(services); // realna parkirišča iz Overpass
}

// Izpiše samo število vnosov v vsaki entiteti, katere ime vsebuje "parkir"
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<SmartParkContext>();
    var parkirTypes = ctx.Model.GetEntityTypes()
        .Select(e => e.ClrType)
        .Where(t => t.Name.ToLower().Contains("parkir"))
        .ToList();

    foreach (var clrType in parkirTypes)
    {
        try
        {
            // Pridobimo DbSet<T> preko reflection (Set<T> kot generična metoda)
            var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes);
            if (setMethod == null)
            {
                Console.WriteLine($"Ne najdem metode DbContext.Set za {clrType.Name}.");
                continue;
            }

            var genericSet = setMethod.MakeGenericMethod(clrType).Invoke(ctx, null);
            if (genericSet == null)
            {
                Console.WriteLine($"DbSet<{clrType.Name}> je null.");
                continue;
            }

            // Preverimo, da imamo IQueryable (potrebno za Queryable.Count)
            if (!(genericSet is System.Linq.IQueryable))
            {
                Console.WriteLine($"{clrType.Name} ni IQueryable.");
                continue;
            }

            // Pokličemo Queryable.Count<T>(IQueryable<T>) preko reflection, da dobimo število vnosov
            var countMethod = typeof(System.Linq.Queryable)
                .GetMethods()
                .First(m => m.Name == "Count" && m.GetParameters().Length == 1)
                .MakeGenericMethod(clrType);

            var countObj = countMethod.Invoke(null, new object[] { genericSet });
            var count = countObj is int i ? i : Convert.ToInt32(countObj ?? 0);

            Console.WriteLine($"--- {clrType.Name}: {count} vnosov ---");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Napaka pri štetju {clrType.Name}: {ex.Message}");
        }
    }
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


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
