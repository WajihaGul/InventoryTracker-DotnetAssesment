using InventoryTracker.Web.Data;
using InventoryTracker.Web.Models;
using InventoryTracker.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryTracker.Tests
{
    public class ZeroFloorRuleTests
    {
        private static (AppDbContext db, SqliteConnection connection) CreateSqliteDb()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new AppDbContext(options);
            db.Database.EnsureCreated();

            return (db, connection);
        }

        private static Microsoft.Extensions.Logging.ILogger<StockService> NullLogger()
            => Microsoft.Extensions.Logging.Abstractions.NullLogger<StockService>.Instance;

        [Fact]
        public async Task RecordMovement_OutExceedingStock_IsRejected()
        {
            var (db, connection) = CreateSqliteDb();
            using (connection)
            using (db)
            {
                var product = new Product { Sku = "TEST-003", Name = "Test Product", IsActive = true };
                db.Products.Add(product);
                await db.SaveChangesAsync();

                db.StockMovements.Add(
                    new StockMovement { ProductId = product.Id, Type = MovementType.In, Quantity = 10 });
                await db.SaveChangesAsync();

                var service = new StockService(db, NullLogger());

                var result = await service.RecordMovementAsync(product.Id, MovementType.Out, 11, null);

                Assert.False(result.IsSuccess);
                Assert.Contains("Only 10 available", result.ErrorMessage);
            }
        }

        [Fact]
        public async Task RecordMovement_OutEqualToStock_Succeeds()
        {
            var (db, connection) = CreateSqliteDb();
            using (connection)
            using (db)
            {
                var product = new Product { Sku = "TEST-004", Name = "Test Product", IsActive = true };
                db.Products.Add(product);
                await db.SaveChangesAsync();

                db.StockMovements.Add(
                    new StockMovement { ProductId = product.Id, Type = MovementType.In, Quantity = 10 });
                await db.SaveChangesAsync();

                var service = new StockService(db, NullLogger());

                var result = await service.RecordMovementAsync(product.Id, MovementType.Out, 10, null);

                Assert.True(result.IsSuccess);

                var stockAfter = await service.GetStockLevelAsync(product.Id);
                Assert.Equal(0, stockAfter);
            }
        }

        [Fact]
        public async Task RecordMovement_ZeroQuantity_IsRejected()
        {
            var (db, connection) = CreateSqliteDb();
            using (connection)
            using (db)
            {
                var product = new Product { Sku = "TEST-005", Name = "Test Product", IsActive = true };
                db.Products.Add(product);
                await db.SaveChangesAsync();

                var service = new StockService(db, NullLogger());

                var result = await service.RecordMovementAsync(product.Id, MovementType.In, 0, null);

                Assert.False(result.IsSuccess);
            }
        }

        [Fact]
        public async Task RecordMovement_InactiveProduct_IsRejected()
        {
            var (db, connection) = CreateSqliteDb();
            using (connection)
            using (db)
            {
                var product = new Product { Sku = "TEST-006", Name = "Test Product", IsActive = false };
                db.Products.Add(product);
                await db.SaveChangesAsync();

                var service = new StockService(db, NullLogger());

                var result = await service.RecordMovementAsync(product.Id, MovementType.In, 5, null);

                Assert.False(result.IsSuccess);
                Assert.Contains("not found or inactive", result.ErrorMessage);
            }
        }
    }
}