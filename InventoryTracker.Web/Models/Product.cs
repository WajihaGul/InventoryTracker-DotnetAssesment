using System.ComponentModel.DataAnnotations;

namespace InventoryTracker.Web.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "SKU is required.")]
        [MaxLength(50)]
        [Display(Name = "SKU")]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(75)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Reorder level cannot be negative.")]
        [Display(Name = "Reorder Level")]
        public int ReorderLevel { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
    }
}
