namespace CeramicERP.Models
{
    public class PaymentSummaryViewModel
    {
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalSalesValue { get; set; }
        public decimal TotalPurchaseValue { get; set; }
        public decimal TotalReceivable { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal CashInHand { get; set; }
        public int CreditTransactions { get; set; }
        public int DebitTransactions { get; set; }
        public int TotalTransactions => CreditTransactions + DebitTransactions;
        public decimal NetCashFlow => TotalCredit - TotalDebit;
        public decimal BusinessPosition => CashInHand + TotalReceivable - TotalPayable;
        public List<PaymentActivityViewModel> CreditActivities { get; set; } = new();
        public List<PaymentActivityViewModel> DebitActivities { get; set; } = new();
        public List<PaymentActivityViewModel> Activities { get; set; } = new();
    }

    public class PaymentActivityViewModel
    {
        public string Source { get; set; } = string.Empty;
        public string Counterparty { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string BillingReference { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal InvoiceAmount { get; set; }
        public decimal SettledAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public bool IsCredit { get; set; }
        public bool IsManualEntry { get; set; }
    }

    public class PaymentEntryInputViewModel
    {
        public string EntryType { get; set; } = "Credit";
        public string Counterparty { get; set; } = string.Empty;
        public string PaymentType { get; set; } = "Cash";
        public string? BillingReference { get; set; }
        public DateTime EntryDate { get; set; } = DateTime.Now;
        public decimal InvoiceAmount { get; set; }
        public decimal SettledAmount { get; set; }
        public string? Notes { get; set; }
    }
}
