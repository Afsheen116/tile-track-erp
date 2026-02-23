namespace CeramicERP.Models
{
    public class Tile
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public required string Size { get; set; }

        public decimal PricePerBox { get; set; }

        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; } = 10;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}