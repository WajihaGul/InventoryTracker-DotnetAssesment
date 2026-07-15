using System.ComponentModel.DataAnnotations;

namespace InventoryTracker.Web.ViewModels
{
    public class ProductFormVm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "SKU is required.")]
        [MaxLength(50)]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(75)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Reorder level cannot be negative.")]
        public int ReorderLevel { get; set; }

        public bool IsActive { get; set; } = true;
    }
}