namespace CeramicERP.Models
{
    public class NetProfitViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalPurchaseCost { get; set; }
        public decimal NetProfit => TotalRevenue - TotalPurchaseCost;
        public decimal AverageMonthlyProfit { get; set; }
        public string BestMonthLabel { get; set; } = string.Empty;
        public decimal BestMonthProfit { get; set; }
        public List<FinanceMonthlyProfitPointViewModel> MonthlyPerformance { get; set; } = new();
    }
}
