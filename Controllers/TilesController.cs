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
    [HasPermission(PermissionNames.ViewInventory)]
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
            var tiles = await _context.Tiles
                .Include(t => t.Category)
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.TotalTiles = tiles.Count;
            ViewBag.TotalStock = tiles.Sum(t => t.StockQuantity);
            ViewBag.LowStockTiles = tiles.Count(t => t.StockQuantity <= t.LowStockThreshold);
            ViewBag.TotalInventoryValue = tiles.Sum(t => t.StockQuantity * t.PricePerBox);

            return View(tiles);
        }

        [HasPermission(PermissionNames.ViewReports)]
        public async Task<IActionResult> Details(int id, DateTime? fromDate, DateTime? toDate)
        {
            var (normalizedFromDate, normalizedToDate) = NormalizeDateRange(fromDate, toDate);

            var tile = await _context.Tiles
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

            if (tile == null)
            {
                return NotFound();
            }

            var saleItemsQuery = _context.SaleItems
                .Include(si => si.Sale)
                .Where(si => si.TileId == id);

            var purchaseItemsQuery = _context.PurchaseItems
                .Include(pi => pi.Purchase)
                .Where(pi => pi.TileId == id);

            if (normalizedFromDate.HasValue)
            {
                saleItemsQuery = saleItemsQuery
                    .Where(si => si.Sale.SaleDate >= normalizedFromDate.Value);
                purchaseItemsQuery = purchaseItemsQuery
                    .Where(pi => pi.Purchase.PurchaseDate >= normalizedFromDate.Value);
            }

            if (normalizedToDate.HasValue)
            {
                var toDateExclusive = normalizedToDate.Value.AddDays(1);
                saleItemsQuery = saleItemsQuery
                    .Where(si => si.Sale.SaleDate < toDateExclusive);
                purchaseItemsQuery = purchaseItemsQuery
                    .Where(pi => pi.Purchase.PurchaseDate < toDateExclusive);
            }

            var saleItems = await saleItemsQuery
                .OrderByDescending(si => si.Sale.SaleDate)
                .ToListAsync();

            var purchaseItems = await purchaseItemsQuery
                .OrderByDescending(pi => pi.Purchase.PurchaseDate)
                .ToListAsync();

            var transactions = saleItems
                .Select(si =>
                {
                    var lineAmount = si.Price * si.Quantity;
                    var saleTotal = si.Sale.TotalAmount;
                    var ratio = saleTotal > 0 ? lineAmount / saleTotal : 0;

                    return new InventoryTransactionViewModel
                    {
                        Type = "Sale",
                        Date = si.Sale.SaleDate,
                        TileId = si.TileId,
                        TileName = tile.Name,
                        PartyName = si.Sale.CustomerName,
                        PaymentType = si.Sale.PaymentType ?? "N/A",
                        Quantity = si.Quantity,
                        UnitPrice = si.Price,
                        TotalAmount = lineAmount,
                        SettledAmount = Math.Round(si.Sale.PaidAmount * ratio, 2),
                        PendingAmount = Math.Round(si.Sale.DueAmount * ratio, 2)
                    };
                })
                .Concat(purchaseItems.Select(pi =>
                {
                    var lineAmount = pi.Price * pi.Quantity;
                    var purchaseTotal = pi.Purchase.TotalAmount;
                    var ratio = purchaseTotal > 0 ? lineAmount / purchaseTotal : 0;

                    return new InventoryTransactionViewModel
                    {
                        Type = "Purchase",
                        Date = pi.Purchase.PurchaseDate,
                        TileId = pi.TileId,
                        TileName = tile.Name,
                        PartyName = pi.Purchase.SupplierName,
                        PaymentType = pi.Purchase.PaymentType ?? "N/A",
                        Quantity = pi.Quantity,
                        UnitPrice = pi.Price,
                        TotalAmount = lineAmount,
                        SettledAmount = Math.Round(pi.Purchase.PaidAmount * ratio, 2),
                        PendingAmount = Math.Round(pi.Purchase.DueAmount * ratio, 2)
                    };
                }))
                .OrderByDescending(t => t.Date)
                .ToList();

            var model = new TileDetailsViewModel
            {
                TileId = tile.Id,
                CategoryId = tile.CategoryId,
                TileName = tile.Name,
                CategoryName = tile.Category?.Name ?? "Uncategorized",
                Size = tile.Size,
                CreatedAt = tile.CreatedAt,
                FromDate = normalizedFromDate,
                ToDate = normalizedToDate,
                PricePerBox = tile.PricePerBox,
                StockQuantity = tile.StockQuantity,
                LowStockThreshold = tile.LowStockThreshold,
                SoldQuantity = saleItems.Sum(si => si.Quantity),
                PurchasedQuantity = purchaseItems.Sum(pi => pi.Quantity),
                SalesValue = saleItems.Sum(si => si.Price * si.Quantity),
                PurchaseValue = purchaseItems.Sum(pi => pi.Price * pi.Quantity),
                SalesReceived = transactions
                    .Where(t => t.IsSale)
                    .Sum(t => t.SettledAmount),
                PurchasePaid = transactions
                    .Where(t => !t.IsSale)
                    .Sum(t => t.SettledAmount),
                SalesPending = transactions
                    .Where(t => t.IsSale)
                    .Sum(t => t.PendingAmount),
                PurchasePending = transactions
                    .Where(t => !t.IsSale)
                    .Sum(t => t.PendingAmount),
                Transactions = transactions
            };

            return View(model);
        }

        // CREATE GET
        [HasPermission(PermissionNames.ManageInventory)]
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
        [HasPermission(PermissionNames.ManageInventory)]
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
        [HasPermission(PermissionNames.ManageInventory)]
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

        private static (DateTime? fromDate, DateTime? toDate) NormalizeDateRange(DateTime? fromDate, DateTime? toDate)
        {
            var normalizedFromDate = fromDate?.Date;
            var normalizedToDate = toDate?.Date;

            if (normalizedFromDate.HasValue &&
                normalizedToDate.HasValue &&
                normalizedFromDate > normalizedToDate)
            {
                (normalizedFromDate, normalizedToDate) = (normalizedToDate, normalizedFromDate);
            }

            return (normalizedFromDate, normalizedToDate);
        }
    }
}
