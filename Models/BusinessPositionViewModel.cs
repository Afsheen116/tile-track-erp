namespace CeramicERP.Models
{
    public class BusinessPositionViewModel
    {
        public decimal CashInHand { get; set; }
        public decimal TotalReceivable { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal BusinessPosition => CashInHand + TotalReceivable - TotalPayable;
        public List<FinanceMonthlyCashFlowPointViewModel> MonthlyFlows { get; set; } = new();
    }
}
