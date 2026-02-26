namespace CeramicERP.Models
{
    public class CashInHandViewModel
    {
        public decimal CashInHand { get; set; }
        public decimal TotalCreditInflow { get; set; }
        public decimal TotalDebitOutflow { get; set; }
        public decimal NetCashFlow => TotalCreditInflow - TotalDebitOutflow;
        public List<FinanceMonthlyCashFlowPointViewModel> MonthlyFlows { get; set; } = new();
    }
}
