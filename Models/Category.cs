namespace CeramicERP.Models
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }

        public ICollection<Tile>? Tiles { get; set; }
    }
}