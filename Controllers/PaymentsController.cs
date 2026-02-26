using CeramicERP.Data;
using CeramicERP.Models;
using CeramicERP.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CeramicERP.Controllers
{
    [Authorize]
    [HasPermission(PermissionNames.ManagePayments)]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .AsNoTracking()
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var purchases = await _context.Purchases
                .AsNoTracking()
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            var paymentEntries = await _context.PaymentEntries
                .AsNoTracking()
                .OrderByDescending(p => p.EntryDate)
                .ToListAsync();

            var cashInHand = await _context.CashAccounts
                .AsNoTracking()
                .Select(c => (decimal?)c.Balance)
                .FirstOrDefaultAsync() ?? 0m;

            var creditActivities = sales
                .Select(s => new PaymentActivityViewModel
                {
                    Source = "Sale",
                    Counterparty = string.IsNullOrWhiteSpace(s.CustomerName) ? "Unknown Customer" : s.CustomerName.Trim(),
                    ActivityDate = s.SaleDate,
                    PaymentType = string.IsNullOrWhiteSpace(s.PaymentType) ? "N/A" : s.PaymentType,
                    BillingReference = $"S-{s.Id.ToString("D5", CultureInfo.InvariantCulture)}",
                    InvoiceAmount = s.TotalAmount,
                    SettledAmount = s.PaidAmount,
                    PendingAmount = s.DueAmount,
                    IsCredit = true,
                    IsManualEntry = false
                })
                .Concat(paymentEntries
                    .Where(e => e.EntryType == "Credit")
                    .Select(e => new PaymentActivityViewModel
                    {
                        Source = "Manual Credit",
                        Counterparty = e.Counterparty,
                        ActivityDate = e.EntryDate,
                        PaymentType = e.PaymentType,
                        BillingReference = e.BillingReference ?? string.Empty,
                        Notes = e.Notes ?? string.Empty,
                        InvoiceAmount = e.InvoiceAmount,
                        SettledAmount = e.SettledAmount,
                        PendingAmount = e.DueAmount,
                        IsCredit = true,
                        IsManualEntry = true
                    }))
                .OrderByDescending(a => a.ActivityDate)
                .ToList();

            var debitActivities = purchases
                .Select(p => new PaymentActivityViewModel
                {
                    Source = "Purchase",
                    Counterparty = string.IsNullOrWhiteSpace(p.SupplierName) ? "Unknown Supplier" : p.SupplierName.Trim(),
                    ActivityDate = p.PurchaseDate,
                    PaymentType = string.IsNullOrWhiteSpace(p.PaymentType) ? "N/A" : p.PaymentType,
                    BillingReference = $"P-{p.Id.ToString("D5", CultureInfo.InvariantCulture)}",
                    InvoiceAmount = p.TotalAmount,
                    SettledAmount = p.PaidAmount,
                    PendingAmount = p.DueAmount,
                    IsCredit = false,
                    IsManualEntry = false
                })
                .Concat(paymentEntries
                    .Where(e => e.EntryType == "Debit")
                    .Select(e => new PaymentActivityViewModel
                    {
                        Source = "Manual Debit",
                        Counterparty = e.Counterparty,
                        ActivityDate = e.EntryDate,
                        PaymentType = e.PaymentType,
                        BillingReference = e.BillingReference ?? string.Empty,
                        Notes = e.Notes ?? string.Empty,
                        InvoiceAmount = e.InvoiceAmount,
                        SettledAmount = e.SettledAmount,
                        PendingAmount = e.DueAmount,
                        IsCredit = false,
                        IsManualEntry = true
                    }))
                .OrderByDescending(a => a.ActivityDate)
                .ToList();

            var model = new PaymentSummaryViewModel
            {
                TotalCredit = creditActivities.Sum(a => a.SettledAmount),
                TotalDebit = debitActivities.Sum(a => a.SettledAmount),
                TotalSalesValue = sales.Sum(s => s.TotalAmount),
                TotalPurchaseValue = purchases.Sum(p => p.TotalAmount),
                TotalReceivable = creditActivities.Sum(a => a.PendingAmount),
                TotalPayable = debitActivities.Sum(a => a.PendingAmount),
                CashInHand = cashInHand,
                CreditTransactions = creditActivities.Count,
                DebitTransactions = debitActivities.Count,
                CreditActivities = creditActivities,
                DebitActivities = debitActivities,
                Activities = creditActivities
                    .Concat(debitActivities)
                    .OrderByDescending(a => a.ActivityDate)
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEntry(PaymentEntryInputViewModel entry)
        {
            var entryType = (entry.EntryType ?? string.Empty).Trim();
            var isCredit = string.Equals(entryType, "Credit", StringComparison.OrdinalIgnoreCase);
            var isDebit = string.Equals(entryType, "Debit", StringComparison.OrdinalIgnoreCase);

            if (!isCredit && !isDebit)
            {
                TempData["PaymentEntryError"] = "Invalid entry type selected.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(entry.Counterparty))
            {
                TempData["PaymentEntryError"] = "Billing party name is required.";
                return RedirectToAction(nameof(Index));
            }

            if (entry.InvoiceAmount <= 0)
            {
                TempData["PaymentEntryError"] = "Invoice amount must be greater than zero.";
                return RedirectToAction(nameof(Index));
            }

            if (entry.SettledAmount < 0)
            {
                TempData["PaymentEntryError"] = "Settled amount cannot be negative.";
                return RedirectToAction(nameof(Index));
            }

            if (entry.SettledAmount > entry.InvoiceAmount)
            {
                TempData["PaymentEntryError"] = "Settled amount cannot exceed invoice amount.";
                return RedirectToAction(nameof(Index));
            }

            var dueAmount = entry.InvoiceAmount - entry.SettledAmount;
            var paymentType = string.IsNullOrWhiteSpace(entry.PaymentType) ? "Cash" : entry.PaymentType.Trim();
            var counterparty = entry.Counterparty.Trim();
            var billingReference = string.IsNullOrWhiteSpace(entry.BillingReference) ? null : entry.BillingReference.Trim();
            var notes = string.IsNullOrWhiteSpace(entry.Notes) ? null : entry.Notes.Trim();

            var cash = await _context.CashAccounts.FirstOrDefaultAsync();
            if (cash == null)
            {
                cash = new CashAccount { Balance = 0m };
                _context.CashAccounts.Add(cash);
            }

            if (isCredit)
            {
                cash.Balance += entry.SettledAmount;
            }
            else
            {
                cash.Balance -= entry.SettledAmount;
            }

            _context.PaymentEntries.Add(new PaymentEntry
            {
                Counterparty = counterparty,
                EntryType = isCredit ? "Credit" : "Debit",
                PaymentType = paymentType,
                BillingReference = billingReference,
                EntryDate = entry.EntryDate == default ? DateTime.Now : entry.EntryDate,
                InvoiceAmount = entry.InvoiceAmount,
                SettledAmount = entry.SettledAmount,
                DueAmount = dueAmount,
                Notes = notes,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["PaymentEntrySuccess"] = isCredit
                ? "Credit inflow billing entry has been added."
                : "Debit outflow billing entry has been added.";

            return RedirectToAction(nameof(Index));
        }
    }
}
