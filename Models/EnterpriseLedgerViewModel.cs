namespace CeramicERP.Models
{
    public class EnterpriseLedgerViewModel
    {
        public string EnterpriseName { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal TotalPurchaseCost { get; set; }
        public decimal TotalReceived { get; set; }
        public decimal TotalPaidOut { get; set; }
        public decimal PendingReceivable { get; set; }
        public decimal PendingPayable { get; set; }
        public int SalesTransactions { get; set; }
        public int PurchaseTransactions { get; set; }
        public List<EnterpriseTransactionViewModel> Transactions { get; set; } = new();
    }

    public class EnterpriseTransactionViewModel
    {
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal SettledAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public int ItemsCount { get; set; }
        public int TotalQuantity { get; set; }
        public bool IsSale => string.Equals(Type, "Sale", StringComparison.OrdinalIgnoreCase);
    }
}
