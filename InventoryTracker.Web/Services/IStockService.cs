using InventoryTracker.Web.Models;

namespace InventoryTracker.Web.Services
{
    public interface IStockService
    {
        Task<int> GetStockLevelAsync(int productId);
        Task<Dictionary<int, int>> GetStockLevelsAsync();
        Task<MovementResult> RecordMovementAsync(int productId, MovementType type, int quantity, string? note);
    }
}