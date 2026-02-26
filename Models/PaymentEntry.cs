using System.ComponentModel.DataAnnotations;

namespace CeramicERP.Models
{
    public class PaymentEntry
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Counterparty { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string EntryType { get; set; } = "Credit";

        [Required]
        [StringLength(30)]
        public string PaymentType { get; set; } = "Cash";

        [StringLength(80)]
        public string? BillingReference { get; set; }

        public DateTime EntryDate { get; set; } = DateTime.Now;

        public decimal InvoiceAmount { get; set; }
        public decimal SettledAmount { get; set; }
        public decimal DueAmount { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
