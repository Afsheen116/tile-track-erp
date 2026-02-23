using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CeramicERP.Models;
using CeramicERP.Data;

namespace CeramicERP.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalCategories = await _context.Categories.CountAsync();
        var totalTiles = await _context.Tiles.CountAsync();
        var totalStock = await _context.Tiles.SumAsync(t => t.StockQuantity);
        var lowStock = await _context.Tiles
            .Where(t => t.StockQuantity <= t.LowStockThreshold)
            .CountAsync();
        var totalRevenue = (await _context.Sales
            .Select(s => s.TotalAmount)
            .ToListAsync())
            .Sum();

        var totalPurchaseCost = (await _context.Purchases
            .Select(p => p.TotalAmount)
            .ToListAsync())
            .Sum();
        var profit = totalRevenue - totalPurchaseCost;
        // Get Cash Balance
        var cashAccount = await _context.CashAccounts.FirstOrDefaultAsync();
        decimal cashBalance = cashAccount?.Balance ?? 0;
        var totalReceivable = _context.Sales
    .AsEnumerable()
    .Sum(s => s.DueAmount);

        var totalPayable = _context.Purchases
            .AsEnumerable()
            .Sum(p => p.DueAmount);
            var businessPosition = cashBalance + totalReceivable - totalPayable;

ViewBag.BusinessPosition = businessPosition;

        ViewBag.TotalReceivable = totalReceivable;
        ViewBag.TotalPayable = totalPayable;

        ViewBag.CashBalance = cashBalance;

        ViewBag.TotalCategories = totalCategories;
        ViewBag.TotalTiles = totalTiles;
        ViewBag.TotalStock = totalStock;
        ViewBag.LowStock = lowStock;

        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.TotalPurchaseCost = totalPurchaseCost;
        ViewBag.Profit = profit;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
