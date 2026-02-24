using CeramicERP.Data;
using CeramicERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace CeramicERP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EnterpriseLedgerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EnterpriseLedgerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Details(string enterpriseName, DateTime? fromDate, DateTime? toDate)
        {
            if (string.IsNullOrWhiteSpace(enterpriseName))
            {
                return RedirectToAction(nameof(SalesController.Index), "Sales");
            }

            var model = await BuildLedgerModel(enterpriseName, fromDate, toDate);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Export(string enterpriseName, DateTime? fromDate, DateTime? toDate)
        {
            if (string.IsNullOrWhiteSpace(enterpriseName))
            {
                return RedirectToAction(nameof(SalesController.Index), "Sales");
            }

            var model = await BuildLedgerModel(enterpriseName, fromDate, toDate);
            var csv = new StringBuilder();

            csv.AppendLine("Date,Type,Payment Type,Total,Settled,Pending,Items,Quantity");

            foreach (var transaction in model.Transactions.OrderBy(t => t.Date))
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(transaction.Date.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCsv(transaction.Type),
                    EscapeCsv(transaction.PaymentType),
                    transaction.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    transaction.SettledAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    transaction.PendingAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    transaction.ItemsCount.ToString(),
                    transaction.TotalQuantity.ToString()));
            }

            var safeEnterprise = string.Join("_",
                model.EnterpriseName
                    .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrWhiteSpace(safeEnterprise))
            {
                safeEnterprise = "enterprise";
            }

            var fromPart = model.FromDate?.ToString("yyyyMMdd") ?? "all";
            var toPart = model.ToDate?.ToString("yyyyMMdd") ?? "all";
            var fileName = $"ledger_{safeEnterprise}_{fromPart}_{toPart}.csv";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(bytes, "text/csv", fileName);
        }

        private async Task<EnterpriseLedgerViewModel> BuildLedgerModel(string enterpriseName, DateTime? fromDate, DateTime? toDate)
        {
            var normalizedEnterpriseName = enterpriseName.Trim().ToLower();
            var filterFromDate = fromDate?.Date;
            var filterToDate = toDate?.Date;

            if (filterFromDate.HasValue && filterToDate.HasValue && filterFromDate > filterToDate)
            {
                (filterFromDate, filterToDate) = (filterToDate, filterFromDate);
            }

            var salesQuery = _context.Sales
                .Include(s => s.Items)
                .Where(s =>
                    s.CustomerName != null &&
                    s.CustomerName.Trim().ToLower() == normalizedEnterpriseName);

            var purchasesQuery = _context.Purchases
                .Include(p => p.Items)
                .Where(p =>
                    p.SupplierName != null &&
                    p.SupplierName.Trim().ToLower() == normalizedEnterpriseName);

            if (filterFromDate.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.SaleDate >= filterFromDate.Value);
                purchasesQuery = purchasesQuery.Where(p => p.PurchaseDate >= filterFromDate.Value);
            }

            if (filterToDate.HasValue)
            {
                var toDateExclusive = filterToDate.Value.AddDays(1);
                salesQuery = salesQuery.Where(s => s.SaleDate < toDateExclusive);
                purchasesQuery = purchasesQuery.Where(p => p.PurchaseDate < toDateExclusive);
            }

            var sales = await salesQuery
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var purchases = await purchasesQuery
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            var transactions = sales
                .Select(s => new EnterpriseTransactionViewModel
                {
                    Type = "Sale",
                    Date = s.SaleDate,
                    PaymentType = s.PaymentType ?? "N/A",
                    TotalAmount = s.TotalAmount,
                    SettledAmount = s.PaidAmount,
                    PendingAmount = s.DueAmount,
                    ItemsCount = s.Items?.Count ?? 0,
                    TotalQuantity = s.Items?.Sum(i => i.Quantity) ?? 0
                })
                .Concat(purchases.Select(p => new EnterpriseTransactionViewModel
                {
                    Type = "Purchase",
                    Date = p.PurchaseDate,
                    PaymentType = p.PaymentType ?? "N/A",
                    TotalAmount = p.TotalAmount,
                    SettledAmount = p.PaidAmount,
                    PendingAmount = p.DueAmount,
                    ItemsCount = p.Items?.Count ?? 0,
                    TotalQuantity = p.Items?.Sum(i => i.Quantity) ?? 0
                }))
                .OrderByDescending(t => t.Date)
                .ToList();

            return new EnterpriseLedgerViewModel
            {
                EnterpriseName = enterpriseName.Trim(),
                FromDate = filterFromDate,
                ToDate = filterToDate,
                TotalEarned = sales.Sum(s => s.TotalAmount),
                TotalPurchaseCost = purchases.Sum(p => p.TotalAmount),
                TotalReceived = sales.Sum(s => s.PaidAmount),
                TotalPaidOut = purchases.Sum(p => p.PaidAmount),
                PendingReceivable = sales.Sum(s => s.DueAmount),
                PendingPayable = purchases.Sum(p => p.DueAmount),
                SalesTransactions = sales.Count,
                PurchaseTransactions = purchases.Count,
                Transactions = transactions
            };
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
