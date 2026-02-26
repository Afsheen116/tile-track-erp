using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CeramicERP.Models;
using CeramicERP.Data;
using CeramicERP.Security;

namespace CeramicERP.Controllers;

[Authorize]
public class HomeController : Controller
{
    private const int FinancialTrendMonths = 6;
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HasPermission(PermissionNames.ViewDashboard)]
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
        var cashAccount = await _context.CashAccounts.FirstOrDefaultAsync();
        var cashBalance = cashAccount?.Balance ?? 0m;
        var salesReceivable = (await _context.Sales
            .AsNoTracking()
            .Select(s => s.DueAmount)
            .ToListAsync())
            .Sum();

        var purchasePayable = (await _context.Purchases
            .AsNoTracking()
            .Select(p => p.DueAmount)
            .ToListAsync())
            .Sum();

        var manualReceivable = (await _context.PaymentEntries
            .AsNoTracking()
            .Where(e => e.EntryType == "Credit")
            .Select(e => e.DueAmount)
            .ToListAsync())
            .Sum();

        var manualPayable = (await _context.PaymentEntries
            .AsNoTracking()
            .Where(e => e.EntryType == "Debit")
            .Select(e => e.DueAmount)
            .ToListAsync())
            .Sum();

        var totalReceivable = salesReceivable + manualReceivable;
        var totalPayable = purchasePayable + manualPayable;
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

    [HasPermission(PermissionNames.ViewDashboardFinancial)]
    public async Task<IActionResult> CashInHand()
    {
        var monthlyFlows = await BuildMonthlyCashFlowAsync(FinancialTrendMonths);
        var cashAccount = await _context.CashAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync();

        var model = new CashInHandViewModel
        {
            CashInHand = cashAccount?.Balance ?? 0m,
            TotalCreditInflow = monthlyFlows.Sum(m => m.CreditInflow),
            TotalDebitOutflow = monthlyFlows.Sum(m => m.DebitOutflow),
            MonthlyFlows = monthlyFlows
        };

        return View(model);
    }

    [HasPermission(PermissionNames.ViewDashboardFinancial)]
    public async Task<IActionResult> BusinessPosition()
    {
        var cashAccount = await _context.CashAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync();
        var cashInHand = cashAccount?.Balance ?? 0m;

        var salesReceivable = (await _context.Sales
            .AsNoTracking()
            .Select(s => s.DueAmount)
            .ToListAsync())
            .Sum();

        var purchasePayable = (await _context.Purchases
            .AsNoTracking()
            .Select(p => p.DueAmount)
            .ToListAsync())
            .Sum();

        var manualReceivable = (await _context.PaymentEntries
            .AsNoTracking()
            .Where(e => e.EntryType == "Credit")
            .Select(e => e.DueAmount)
            .ToListAsync())
            .Sum();

        var manualPayable = (await _context.PaymentEntries
            .AsNoTracking()
            .Where(e => e.EntryType == "Debit")
            .Select(e => e.DueAmount)
            .ToListAsync())
            .Sum();

        var model = new BusinessPositionViewModel
        {
            CashInHand = cashInHand,
            TotalReceivable = salesReceivable + manualReceivable,
            TotalPayable = purchasePayable + manualPayable,
            MonthlyFlows = await BuildMonthlyCashFlowAsync(FinancialTrendMonths)
        };

        return View(model);
    }

    [HasPermission(PermissionNames.ViewProfit)]
    public async Task<IActionResult> NetProfit()
    {
        var totalRevenue = (await _context.Sales
            .AsNoTracking()
            .Select(s => s.TotalAmount)
            .ToListAsync())
            .Sum();

        var totalPurchaseCost = (await _context.Purchases
            .AsNoTracking()
            .Select(p => p.TotalAmount)
            .ToListAsync())
            .Sum();

        var monthlyPerformance = await BuildMonthlyProfitAsync(FinancialTrendMonths);
        var bestMonth = monthlyPerformance
            .OrderByDescending(m => m.NetProfit)
            .FirstOrDefault();

        var model = new NetProfitViewModel
        {
            TotalRevenue = totalRevenue,
            TotalPurchaseCost = totalPurchaseCost,
            AverageMonthlyProfit = monthlyPerformance.Any()
                ? monthlyPerformance.Average(m => m.NetProfit)
                : 0m,
            BestMonthLabel = bestMonth?.MonthLabel ?? "N/A",
            BestMonthProfit = bestMonth?.NetProfit ?? 0m,
            MonthlyPerformance = monthlyPerformance
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
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

    private async Task<List<FinanceMonthlyCashFlowPointViewModel>> BuildMonthlyCashFlowAsync(int months)
    {
        var monthWindow = BuildMonthWindow(months);
        var startDate = monthWindow[0];

        var salesPaid = await _context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= startDate)
            .Select(s => new { s.SaleDate, s.PaidAmount })
            .ToListAsync();

        var purchasesPaid = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.PurchaseDate >= startDate)
            .Select(p => new { p.PurchaseDate, p.PaidAmount })
            .ToListAsync();

        var manualCredits = await _context.PaymentEntries
            .AsNoTracking()
            .Where(e => e.EntryDate >= startDate && e.EntryType == "Credit")
            .Select(e => new { e.EntryDate, e.SettledAmount })
            .ToListAsync();

        var manualDebits = await _context.PaymentEntries
            .AsNoTracking()
            .Where(e => e.EntryDate >= startDate && e.EntryType == "Debit")
            .Select(e => new { e.EntryDate, e.SettledAmount })
            .ToListAsync();

        var salesByMonth = GroupByMonth(
            salesPaid,
            row => row.SaleDate,
            row => row.PaidAmount);
        var purchasesByMonth = GroupByMonth(
            purchasesPaid,
            row => row.PurchaseDate,
            row => row.PaidAmount);
        var manualCreditsByMonth = GroupByMonth(
            manualCredits,
            row => row.EntryDate,
            row => row.SettledAmount);
        var manualDebitsByMonth = GroupByMonth(
            manualDebits,
            row => row.EntryDate,
            row => row.SettledAmount);

        return monthWindow
            .Select(month =>
            {
                var creditInflow =
                    GetMonthValue(salesByMonth, month) +
                    GetMonthValue(manualCreditsByMonth, month);

                var debitOutflow =
                    GetMonthValue(purchasesByMonth, month) +
                    GetMonthValue(manualDebitsByMonth, month);

                return new FinanceMonthlyCashFlowPointViewModel
                {
                    MonthLabel = month.ToString("MMM yyyy"),
                    CreditInflow = creditInflow,
                    DebitOutflow = debitOutflow
                };
            })
            .ToList();
    }

    private async Task<List<FinanceMonthlyProfitPointViewModel>> BuildMonthlyProfitAsync(int months)
    {
        var monthWindow = BuildMonthWindow(months);
        var startDate = monthWindow[0];

        var sales = await _context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= startDate)
            .Select(s => new { s.SaleDate, s.TotalAmount })
            .ToListAsync();

        var purchases = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.PurchaseDate >= startDate)
            .Select(p => new { p.PurchaseDate, p.TotalAmount })
            .ToListAsync();

        var revenueByMonth = GroupByMonth(
            sales,
            row => row.SaleDate,
            row => row.TotalAmount);
        var purchaseCostByMonth = GroupByMonth(
            purchases,
            row => row.PurchaseDate,
            row => row.TotalAmount);

        return monthWindow
            .Select(month => new FinanceMonthlyProfitPointViewModel
            {
                MonthLabel = month.ToString("MMM yyyy"),
                Revenue = GetMonthValue(revenueByMonth, month),
                PurchaseCost = GetMonthValue(purchaseCostByMonth, month)
            })
            .ToList();
    }

    private static List<DateTime> BuildMonthWindow(int months)
    {
        var monthCount = Math.Max(months, 1);
        var firstMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            .AddMonths(-(monthCount - 1));

        return Enumerable.Range(0, monthCount)
            .Select(offset => firstMonth.AddMonths(offset))
            .ToList();
    }

    private static Dictionary<DateTime, decimal> GroupByMonth<T>(
        IEnumerable<T> source,
        Func<T, DateTime> dateSelector,
        Func<T, decimal> amountSelector)
    {
        return source
            .Select(item => new
            {
                Month = new DateTime(dateSelector(item).Year, dateSelector(item).Month, 1),
                Amount = amountSelector(item)
            })
            .GroupBy(item => item.Month)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));
    }

    private static decimal GetMonthValue(
        IReadOnlyDictionary<DateTime, decimal> monthMap,
        DateTime month)
    {
        return monthMap.TryGetValue(month, out var value) ? value : 0m;
    }
}
