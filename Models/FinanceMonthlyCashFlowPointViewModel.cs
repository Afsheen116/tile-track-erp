namespace CeramicERP.Models
{
    public class FinanceMonthlyCashFlowPointViewModel
    {
        public string MonthLabel { get; set; } = string.Empty;
        public decimal CreditInflow { get; set; }
        public decimal DebitOutflow { get; set; }
        public decimal NetCashFlow => CreditInflow - DebitOutflow;
    }
}
