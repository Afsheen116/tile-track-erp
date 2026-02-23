using System.ComponentModel.DataAnnotations;

namespace CeramicERP.Models
{
    public class Sale
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        public List<SaleItem> Items { get; set; }
        public string PaymentType { get; set; } = "Cash";
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
    }
}