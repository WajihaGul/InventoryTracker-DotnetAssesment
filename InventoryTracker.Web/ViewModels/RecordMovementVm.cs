using System.ComponentModel.DataAnnotations;
using InventoryTracker.Web.Models;

namespace InventoryTracker.Web.ViewModels
{
    public class RecordMovementVm
    {
        public int ProductId { get; set; }

        [Required]
        public MovementType Type { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int Quantity { get; set; }

        [MaxLength(200)]
        public string? Note { get; set; }
    }
}