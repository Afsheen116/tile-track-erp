using CeramicERP.Data;
using CeramicERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CeramicERP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LIST
        public async Task<IActionResult> Index()
        {
            var categoryEntities = await _context.Categories
                .Include(c => c.Tiles)
                .AsNoTracking()
                .ToListAsync();

            var categories = categoryEntities
                .Select(c =>
                {
                    var activeTiles = (c.Tiles ?? new List<Tile>())
                        .Where(t => !t.IsDeleted)
                        .ToList();
                    var tileCount = activeTiles.Count;

                    return new CategoryIndexItemViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        TileCount = tileCount,
                        TotalStock = activeTiles.Sum(t => t.StockQuantity),
                        LowStockTiles = activeTiles.Count(t => t.StockQuantity <= t.LowStockThreshold),
                        AveragePrice = tileCount > 0
                            ? activeTiles.Sum(t => t.PricePerBox) / tileCount
                            : 0,
                        InventoryValue = activeTiles.Sum(t => t.PricePerBox * t.StockQuantity)
                    };
                })
                .OrderBy(c => c.Name)
                .ToList();

            ViewBag.TotalCategories = categories.Count;
            ViewBag.TotalTiles = categories.Sum(c => c.TileCount);
            ViewBag.TotalStock = categories.Sum(c => c.TotalStock);
            ViewBag.TotalInventoryValue = categories.Sum(c => c.InventoryValue);

            return View(categories);
        }

        public async Task<IActionResult> Details(int id, DateTime? fromDate, DateTime? toDate)
        {
            var (normalizedFromDate, normalizedToDate) = NormalizeDateRange(fromDate, toDate);

            var category = await _context.Categories
                .Include(c => c.Tiles!.Where(t => !t.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            var categoryTiles = category.Tiles?
                .OrderBy(t => t.Name)
                .ToList() ?? new List<Tile>();

            var tileIds = categoryTiles.Select(t => t.Id).ToList();

            var saleItemsQuery = _context.SaleItems
                .Include(si => si.Sale)
                .Include(si => si.Tile)
                .Where(si => tileIds.Contains(si.TileId));

            var purchaseItemsQuery = _context.PurchaseItems
                .Include(pi => pi.Purchase)
                .Include(pi => pi.Tile)
                .Where(pi => tileIds.Contains(pi.TileId));

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
                        TileName = si.Tile?.Name ?? "Tile",
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
                        TileName = pi.Tile?.Name ?? "Tile",
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

            var model = new CategoryDetailsViewModel
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                FromDate = normalizedFromDate,
                ToDate = normalizedToDate,
                TotalTiles = categoryTiles.Count,
                TotalStock = categoryTiles.Sum(t => t.StockQuantity),
                LowStockTiles = categoryTiles.Count(t => t.StockQuantity <= t.LowStockThreshold),
                AveragePrice = categoryTiles.Count > 0
                    ? categoryTiles.Average(t => t.PricePerBox)
                    : 0,
                InventoryValue = categoryTiles.Sum(t => t.PricePerBox * t.StockQuantity),
                SoldQuantity = saleItems.Sum(si => si.Quantity),
                PurchasedQuantity = purchaseItems.Sum(pi => pi.Quantity),
                SalesValue = saleItems.Sum(si => si.Price * si.Quantity),
                PurchaseValue = purchaseItems.Sum(pi => pi.Price * pi.Quantity),
                Tiles = categoryTiles.Select(t => new CategoryTileSummaryViewModel
                {
                    TileId = t.Id,
                    TileName = t.Name,
                    Size = t.Size,
                    PricePerBox = t.PricePerBox,
                    StockQuantity = t.StockQuantity,
                    LowStockThreshold = t.LowStockThreshold
                }).ToList(),
                Transactions = transactions
            };

            return View(model);
        }

        // CREATE GET
        public IActionResult Create()
        {
            return View();
        }

        // CREATE POST
        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
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
