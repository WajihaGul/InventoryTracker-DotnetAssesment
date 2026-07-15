using InventoryTracker.Web.Models;

namespace InventoryTracker.Web.ViewModels
{
    public class ProductDetailVm
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ReorderLevel { get; set; }
        public int StockLevel { get; set; }
        public bool IsLowStock => StockLevel <= ReorderLevel;
        public List<StockMovement> Movements { get; set; } = new();

        public RecordMovementVm Movement { get; set; } = new();
    }
}