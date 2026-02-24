using CeramicERP.Data;
using CeramicERP.Models;
using CeramicERP.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CeramicERP.Controllers
{
    [Authorize]
    [HasPermission(PermissionNames.ViewPurchases)]
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PurchasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var purchases = await _context.Purchases
                .Include(p => p.Items)
                .ThenInclude(i => i.Tile)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            ViewBag.TotalCost = purchases.Sum(p => p.TotalAmount);
            ViewBag.TotalPaid = purchases.Sum(p => p.PaidAmount);
            ViewBag.TotalPending = purchases.Sum(p => p.DueAmount);
            ViewBag.UniqueSuppliers = purchases
                .Select(p => p.SupplierName?.Trim().ToLower())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .Count();

            return View(purchases);
        }

        [HasPermission(PermissionNames.CreatePurchase)]
        public IActionResult Create()
        {
            ViewBag.Tiles = new SelectList(
                _context.Tiles.Where(t => !t.IsDeleted),
                "Id",
                "Name"
            );

            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [HasPermission(PermissionNames.CreatePurchase)]
        public async Task<IActionResult> Create(
            string supplierName,
            int tileId,
            int quantity,
            decimal price,
            string paymentType,
            decimal paidAmount)
        {
            var tile = await _context.Tiles.FindAsync(tileId);

            if (tile == null)
            {
                ModelState.AddModelError("", "Invalid tile selected.");
            }
            else if (quantity <= 0)
            {
                ModelState.AddModelError("", "Quantity must be greater than zero.");
            }

            decimal totalAmount = quantity * price;

            if (paymentType == "Cash")
            {
                paidAmount = totalAmount;
            }
            else if (paymentType == "Credit")
            {
                paidAmount = 0;
            }
            else if (paymentType == "Partial" && paidAmount > totalAmount)
            {
                ModelState.AddModelError("", "Paid amount cannot exceed total amount.");
            }

            decimal dueAmount = totalAmount - paidAmount;

            if (!ModelState.IsValid)
            {
                ViewBag.Tiles = new SelectList(
                    _context.Tiles.Where(t => !t.IsDeleted),
                    "Id",
                    "Name"
                );
                return View();
            }

            var selectedTile = tile!;
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var purchase = new Purchase
                {
                    SupplierName = supplierName,
                    PurchaseDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    PaidAmount = paidAmount,
                    DueAmount = dueAmount,
                    PaymentType = paymentType,
                    Items = new List<PurchaseItem>()
                };

                var item = new PurchaseItem
                {
                    TileId = tileId,
                    Quantity = quantity,
                    Price = price
                };

                purchase.Items.Add(item);

                selectedTile.StockQuantity += quantity;

                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Something went wrong.");
                return View();
            }
        }
    }
}
