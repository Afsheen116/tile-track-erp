namespace CeramicERP.Models
{
    public class SaleViewModel
    {
        public string CustomerName { get; set; }
        public int TileId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string PaymentType { get; set; } = "Cash";
        public decimal PaidAmount { get; set; }
    }
}