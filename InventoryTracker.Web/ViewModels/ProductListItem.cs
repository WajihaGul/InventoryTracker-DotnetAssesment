namespace InventoryTracker.Web.ViewModels
{
    public class ProductListItemVm
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int StockLevel { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsLowStock => StockLevel <= ReorderLevel;
    }
}