using CeramicERP.Data;
using CeramicERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CeramicERP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .Include(s => s.Items)
                .ThenInclude(i => i.Tile)
                .ToListAsync();

            return View(sales);
        }

        public IActionResult Create()
        {
            ViewBag.Tiles = new SelectList(
                _context.Tiles.Where(t => !t.IsDeleted),
                "Id",
                "Name"
            );

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HttpPost]

        public async Task<IActionResult> Create(SaleViewModel model)
        {
            var tile = await _context.Tiles.FindAsync(model.TileId);

            if (string.IsNullOrWhiteSpace(model.CustomerName))
                ModelState.AddModelError("", "Customer name is required.");

            if (tile == null)
                ModelState.AddModelError("", "Invalid tile selected.");
            else if (model.Quantity <= 0)
                ModelState.AddModelError("", "Quantity must be greater than zero.");
            else if (tile.StockQuantity < model.Quantity)
                ModelState.AddModelError("", "Insufficient stock available.");

            if (!ModelState.IsValid)
            {
                ViewBag.Tiles = new SelectList(
                    _context.Tiles.Where(t => !t.IsDeleted),
                    "Id",
                    "Name",
                    model.TileId
                );

                return View(model);
            }

            // 🔐 BEGIN TRANSACTION
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ==========================
                // PAYMENT CALCULATION LOGIC
                // ==========================

                decimal totalAmount = model.Quantity * model.Price;
                decimal paidAmount = 0;
                decimal dueAmount = 0;

                if (model.PaymentType == "Cash")
                {
                    paidAmount = totalAmount;
                    dueAmount = 0;
                }
                else if (model.PaymentType == "Credit")
                {
                    paidAmount = 0;
                    dueAmount = totalAmount;
                }
                else if (model.PaymentType == "Partial")
                {
                    if (model.PaidAmount <= 0 || model.PaidAmount > totalAmount)
                    {
                        ModelState.AddModelError("", "Invalid partial payment amount.");
                    }
                    else
                    {
                        paidAmount = model.PaidAmount;
                        dueAmount = totalAmount - paidAmount;
                    }
                }

                // If payment validation failed
                if (!ModelState.IsValid)
                {
                    await transaction.RollbackAsync();

                    ViewBag.Tiles = new SelectList(
                        _context.Tiles.Where(t => !t.IsDeleted),
                        "Id",
                        "Name",
                        model.TileId
                    );

                    return View(model);
                }

                // ==========================
                // CREATE SALE
                // ==========================

                var sale = new Sale
                {
                    CustomerName = model.CustomerName,
                    SaleDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    PaymentType = model.PaymentType,
                    PaidAmount = paidAmount,
                    DueAmount = dueAmount,
                    Items = new List<SaleItem>()
                };

                var item = new SaleItem
                {
                    TileId = model.TileId,
                    Quantity = model.Quantity,
                    Price = model.Price
                };

                sale.Items.Add(item);

                // Reduce stock
                tile.StockQuantity -= model.Quantity;

                // ==========================
                // UPDATE CASH ACCOUNT
                // ==========================

                var cash = await _context.CashAccounts.FirstOrDefaultAsync();

                if (cash == null)
                {
                    cash = new CashAccount { Balance = 0 };
                    _context.CashAccounts.Add(cash);
                }

                cash.Balance += paidAmount;

                _context.Sales.Add(sale);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // ❌ If anything fails → rollback everything
                await transaction.RollbackAsync();

                ModelState.AddModelError("", "Something went wrong. Transaction rolled back.");

                ViewBag.Tiles = new SelectList(
                    _context.Tiles.Where(t => !t.IsDeleted),
                    "Id",
                    "Name",
                    model.TileId
                );

                return View(model);
            }
        }
    }
}
