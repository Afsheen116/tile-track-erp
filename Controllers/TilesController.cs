using CeramicERP.Data;
using CeramicERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CeramicERP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TilesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LIST
        public async Task<IActionResult> Index()
        {
            var tiles = _context.Tiles
                .Include(t => t.Category)
                .Where(t => !t.IsDeleted);

            return View(await tiles.ToListAsync());
        }

        // CREATE GET
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(
                _context.Categories.ToList(),
                "Id",
                "Name"
            );

            return View();
        }
        // CREATE POST
        [HttpPost]
        public async Task<IActionResult> Create(Tile tile)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(
      _context.Categories.ToList(),
      "Id",
      "Name",
      tile.CategoryId
            );
            return View(tile);
        }

        // SOFT DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var tile = await _context.Tiles.FindAsync(id);
            if (tile != null)
            {
                tile.IsDeleted = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}