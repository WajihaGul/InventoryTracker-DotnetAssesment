using InventoryTracker.Web.Data;
using InventoryTracker.Web.Models;
using InventoryTracker.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryTracker.Tests
{
    public class StockCalculationTests
    {
        private static AppDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetStockLevelAsync_NoMovements_ReturnsZero()
        {
            using var db = CreateInMemoryDb();
            var product = new Product { Sku = "TEST-001", Name = "Test Product" };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var service = new StockService(db, NullLogger());

            var stock = await service.GetStockLevelAsync(product.Id);

            Assert.Equal(0, stock);
        }

        [Fact]
        public async Task GetStockLevelAsync_SumsInAndOutCorrectly()
        {
            using var db = CreateInMemoryDb();
            var product = new Product { Sku = "TEST-002", Name = "Test Product" };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            db.StockMovements.AddRange(
                new StockMovement { ProductId = product.Id, Type = MovementType.In, Quantity = 100 },
                new StockMovement { ProductId = product.Id, Type = MovementType.Out, Quantity = 30 },
                new StockMovement { ProductId = product.Id, Type = MovementType.In, Quantity = 20 }
            );
            await db.SaveChangesAsync();

            var service = new StockService(db, NullLogger());

            var stock = await service.GetStockLevelAsync(product.Id);

            Assert.Equal(90, stock); // 100 - 30 + 20
        }

        [Fact]
        public async Task GetStockLevelsAsync_ReturnsCorrectLevelsForMultipleProducts()
        {
            using var db = CreateInMemoryDb();
            var productA = new Product { Sku = "TEST-A", Name = "A" };
            var productB = new Product { Sku = "TEST-B", Name = "B" };
            db.Products.AddRange(productA, productB);
            await db.SaveChangesAsync();

            db.StockMovements.AddRange(
                new StockMovement { ProductId = productA.Id, Type = MovementType.In, Quantity = 50 },
                new StockMovement { ProductId = productB.Id, Type = MovementType.In, Quantity = 10 },
                new StockMovement { ProductId = productB.Id, Type = MovementType.Out, Quantity = 4 }
            );
            await db.SaveChangesAsync();

            var service = new StockService(db, NullLogger());

            var levels = await service.GetStockLevelsAsync();

            Assert.Equal(50, levels[productA.Id]);
            Assert.Equal(6, levels[productB.Id]);
        }

        private static Microsoft.Extensions.Logging.ILogger<StockService> NullLogger()
            => Microsoft.Extensions.Logging.Abstractions.NullLogger<StockService>.Instance;
    }
}