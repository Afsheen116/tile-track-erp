namespace CeramicERP.Models
{
    public class CategoryIndexItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TileCount { get; set; }
        public int TotalStock { get; set; }
        public int LowStockTiles { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal InventoryValue { get; set; }
    }

    public class CategoryDetailsViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int TotalTiles { get; set; }
        public int TotalStock { get; set; }
        public int LowStockTiles { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal InventoryValue { get; set; }
        public int SoldQuantity { get; set; }
        public int PurchasedQuantity { get; set; }
        public decimal SalesValue { get; set; }
        public decimal PurchaseValue { get; set; }
        public List<CategoryTileSummaryViewModel> Tiles { get; set; } = new();
        public List<InventoryTransactionViewModel> Transactions { get; set; } = new();
    }

    public class CategoryTileSummaryViewModel
    {
        public int TileId { get; set; }
        public string TileName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public decimal PricePerBox { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public bool IsLowStock => StockQuantity <= LowStockThreshold;
    }

    public class TileDetailsViewModel
    {
        public int TileId { get; set; }
        public int CategoryId { get; set; }
        public string TileName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal PricePerBox { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public int SoldQuantity { get; set; }
        public int PurchasedQuantity { get; set; }
        public decimal SalesValue { get; set; }
        public decimal PurchaseValue { get; set; }
        public decimal SalesReceived { get; set; }
        public decimal PurchasePaid { get; set; }
        public decimal SalesPending { get; set; }
        public decimal PurchasePending { get; set; }
        public List<InventoryTransactionViewModel> Transactions { get; set; } = new();
        public bool IsLowStock => StockQuantity <= LowStockThreshold;
    }

    public class InventoryTransactionViewModel
    {
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int TileId { get; set; }
        public string TileName { get; set; } = string.Empty;
        public string PartyName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = "N/A";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SettledAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public bool IsSale => string.Equals(Type, "Sale", StringComparison.OrdinalIgnoreCase);
    }
}
