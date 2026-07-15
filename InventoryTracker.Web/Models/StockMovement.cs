using System.ComponentModel.DataAnnotations;

namespace InventoryTracker.Web.Models
{
    public class StockMovement
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Required]
        public MovementType Type { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int Quantity { get; set; }

        [MaxLength(200)]
        public string? Note { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
