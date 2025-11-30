using Microsoft.AspNetCore.Identity;
using SmartPark.Models;

namespace SmartPark.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var ctx = scope.ServiceProvider.GetRequiredService<SmartParkContext>();

        // Vloge
        var roles = new[] { "Administrator", "Manager", "Staff" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Demo uporabnik
        var adminEmail = "admin@smartpark.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Smart",
                LastName = "Admin",
                EmailConfirmed = true,
                LicensePlate = "GO-1234"
            };
            await userManager.CreateAsync(admin, "Admin!234"); // močno geslo
            await userManager.AddToRoleAsync(admin, "Administrator");
        }

        // Demo parkirišče + mesta
        if (!ctx.Parkirisca.Any())
        {
            var p = new Parkirisce
            {
                Naslov = "Trg Republike 1, Ljubljana",
                SteviloMest = 10,
                CenaNaUro = 2.50m,
                DelovniCas = "00:00–24:00"
            };
            ctx.Parkirisca.Add(p);
            await ctx.SaveChangesAsync();

            var mesta = Enumerable.Range(1, 10)
                .Select(i => new ParkirnoMesto
                {
                    ParkirisceId = p.Id,
                    Tip = i <= 2 ? TipMesta.ElektricnoVozilo : (i == 3 ? TipMesta.Invalidsko : TipMesta.Navadno),
                    Zasedeno = false
                }).ToList();

            ctx.ParkirnaMesta.AddRange(mesta);
            await ctx.SaveChangesAsync();
        }
    }
}
