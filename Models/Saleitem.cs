namespace CeramicERP.Models
{
    public class SaleItem
    {
        public int Id { get; set; }

        public int SaleId { get; set; }
        public Sale Sale { get; set; }

        public int TileId { get; set; }
        public Tile Tile { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }
    }
}