using System.Data;
using InventoryTracker.Web.Data;
using InventoryTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Web.Services
{
    public class StockService : IStockService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<StockService> _logger;

        public StockService(AppDbContext db, ILogger<StockService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> GetStockLevelAsync(int productId)
        {
            return await _db.StockMovements
                .Where(m => m.ProductId == productId)
                .SumAsync(m => m.Type == MovementType.In ? m.Quantity : -m.Quantity);
        }

        public async Task<Dictionary<int, int>> GetStockLevelsAsync(IEnumerable<int>? productIdList = null)
        {
            
            var query = _db.StockMovements.AsQueryable();

            if(productIdList != null && productIdList.Any())
            {
                query = query.Where(m => productIdList.Contains(m.ProductId));
            }


return await query

    .GroupBy(m => m.ProductId)
    .Select(g => new
    {
        ProductId = g.Key,
        StockLevel = g.Sum(m => m.Type == MovementType.In ? m.Quantity : -m.Quantity)
    })
    .ToDictionaryAsync(x => x.ProductId, x => x.StockLevel);
}

public async Task<MovementResult> RecordMovementAsync(
int productId, MovementType type, int quantity, string? note)
{
if (quantity <= 0)
    return MovementResult.Fail("Quantity must be greater than zero.");

if (!Enum.IsDefined(type))
    return MovementResult.Fail("Invalid movement type.");

await using var transaction = await _db.Database
    .BeginTransactionAsync(IsolationLevel.Serializable);

try
{
    var productExists = await _db.Products
        .AnyAsync(p => p.Id == productId && p.IsActive);

    if (!productExists)
        return MovementResult.Fail("Product not found or inactive.");

    if (type == MovementType.Out)
    {
        var stockLevel = await _db.StockMovements
            .Where(m => m.ProductId == productId)
            .SumAsync(m => m.Type == MovementType.In ? m.Quantity : -m.Quantity);

        if (quantity > stockLevel)
            return MovementResult.Fail($"Cannot issue {quantity} units. Only {stockLevel} available.");
    }

    _db.StockMovements.Add(new StockMovement
    {
        ProductId = productId,
        Type = type,
        Quantity = quantity,
        Note = note,
        CreatedUtc = DateTime.UtcNow
    });

    await _db.SaveChangesAsync();
    await transaction.CommitAsync();

    return MovementResult.Success();
}
catch (DbUpdateException ex)
{
    await transaction.RollbackAsync();
    _logger.LogWarning(ex, "Concurrent stock update conflict for product {ProductId}", productId);
    return MovementResult.Fail("The stock level changed while processing your request. Please try again.");
}
}
}
}