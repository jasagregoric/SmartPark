using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartPark.Data;
using SmartPark.Models;

namespace SmartPark.Controllers
{
    public class RezervacijeController : Controller
    {
        private readonly SmartParkContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RezervacijeController(SmartParkContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //API endpoint za prvo prosto parkirno mesto
        [HttpGet]
        public async Task<IActionResult> GetProstoMesto(int parkirisceId, DateTime zacetek, DateTime konec)
        {
            var mesto = await _context.ParkirnaMesta
                .Where(pm => pm.ParkirisceId == parkirisceId && pm.Zasedeno == false)
                .Where(pm => !_context.Rezervacije.Any(r =>
                    r.ParkirnoMestoId == pm.Id &&
                    r.Zacetek < konec &&
                    r.Konec > zacetek))
                .OrderBy(pm => pm.Id)
                .Select(pm => pm.Id)
                .FirstOrDefaultAsync();

            return Json(mesto);
        }


        // 🔥 Create rezervacije (če ga še nimaš)
        [HttpPost]
        public async Task<IActionResult> Create(int ParkirisceId, int ParkirnoMestoId, DateTime DatumZacetka, DateTime DatumKonca)
        {
            Console.WriteLine($"DEBUG: ParkirnoMestoId = {ParkirnoMestoId}");

            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Unauthorized();

            var rezervacija = new Rezervacija
            {
                ParkirisceId = ParkirisceId,
                ParkirnoMestoId = ParkirnoMestoId,
                Zacetek = DatumZacetka,
                Konec = DatumKonca,
                ApplicationUserId = userId,
                Status = 0,
                DateCreated = DateTime.Now
            };

            
            var pm = await _context.ParkirnaMesta.FindAsync(ParkirnoMestoId);

            if (pm == null)
            {
                return BadRequest($"Parkirno mesto {ParkirnoMestoId} ne obstaja.");
            }

            if (pm.ParkirisceId != ParkirisceId)
            {
                return BadRequest($"Parkirno mesto {ParkirnoMestoId} ne pripada parkirišču {ParkirisceId}.");
            }

            
            _context.Rezervacije.Add(rezervacija);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

    }
}
