namespace CeramicERP.Models
{
    public class FinanceMonthlyProfitPointViewModel
    {
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal PurchaseCost { get; set; }
        public decimal NetProfit => Revenue - PurchaseCost;
    }
}
