using Microsoft.EntityFrameworkCore;
using InventoryTracker.Web.Models;

namespace InventoryTracker.Web.Data
{
    public class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.Products.AnyAsync())
            {
                return;
            }

            var baseDate = DateTime.UtcNow.AddDays(-30);

            var products = new List<Product>
            {
                new Product
                {
                    Sku = "BOLT-M8-001",
                    Name = "Hex Bolt M8 x 40mm",
                    Description = "Zinc-plated steel hex bolt, 8mm diameter.",
                    ReorderLevel = 50,
                    CreatedUtc = baseDate,
                    Movements = new List<StockMovement>
                    {
                        new() { Type = MovementType.In,  Quantity = 500, Note = "Opening stock",        CreatedUtc = baseDate },
                        new() { Type = MovementType.Out, Quantity = 120, Note = "Issued to Job #4412",  CreatedUtc = baseDate.AddDays(5) },
                        new() { Type = MovementType.In,  Quantity = 200, Note = "Supplier delivery",    CreatedUtc = baseDate.AddDays(12) },
                        new() { Type = MovementType.Out, Quantity = 80,  Note = "Issued to Job #4501",  CreatedUtc = baseDate.AddDays(20) }
                    }
                },
                new Product
                {
                    Sku = "NUT-M8-001",
                    Name = "Hex Nut M8",
                    Description = "Standard zinc-plated hex nut.",
                    ReorderLevel = 100,
                    CreatedUtc = baseDate,
                    Movements = new List<StockMovement>
                    {
                        new() { Type = MovementType.In,  Quantity = 300, Note = "Opening stock",       CreatedUtc = baseDate },
                        new() { Type = MovementType.Out, Quantity = 220, Note = "Issued to Job #4412", CreatedUtc = baseDate.AddDays(5) }
                    }
                },
                new Product
                {
                    Sku = "WASH-M8-001",
                    Name = "Flat Washer M8",
                    Description = null,
                    ReorderLevel = 40,
                    CreatedUtc = baseDate,
                    Movements = new List<StockMovement>
                    {
                        new() { Type = MovementType.In,  Quantity = 150, Note = "Opening stock",       CreatedUtc = baseDate },
                        new() { Type = MovementType.Out, Quantity = 110, Note = "Issued to Job #4488", CreatedUtc = baseDate.AddDays(8) }
                    }
                },
                new Product
                {
                    Sku = "BEAR-6204-001",
                    Name = "Ball Bearing 6204-2RS",
                    Description = "Sealed deep groove ball bearing, 20mm bore.",
                    ReorderLevel = 10,
                    CreatedUtc = baseDate,
                    Movements = new List<StockMovement>
                    {
                        new() { Type = MovementType.In,  Quantity = 40, Note = "Opening stock",       CreatedUtc = baseDate },
                        new() { Type = MovementType.Out, Quantity = 40, Note = "Issued to Job #4390", CreatedUtc = baseDate.AddDays(15) }
                    }
                },
                new Product
                {
                    Sku = "SEAL-OR-025",
                    Name = "O-Ring Seal 25mm",
                    Description = "Nitrile rubber O-ring.",
                    ReorderLevel = 25,
                    IsActive = false,
                    CreatedUtc = baseDate,
                    Movements = new List<StockMovement>
                    {
                        new() { Type = MovementType.In, Quantity = 60, Note = "Opening stock", CreatedUtc = baseDate }
                    }
                }
            };

            db.Products.AddRange(products);
            await db.SaveChangesAsync();
        }
    
    }
}
