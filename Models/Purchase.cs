using System.ComponentModel.DataAnnotations;

namespace CeramicERP.Models
{
    public class Purchase
    {
        public int Id { get; set; }

        [Required]
        public string SupplierName { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        public List<PurchaseItem> Items { get; set; }
        public string PaymentType { get; set; } = "Cash";
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        
    }
}