namespace CeramicERP.Models
{
    public class PurchaseItem
    {
        public int Id { get; set; }

        public int PurchaseId { get; set; }
        public Purchase Purchase { get; set; }

        public int TileId { get; set; }
        public Tile Tile { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }
    }
}