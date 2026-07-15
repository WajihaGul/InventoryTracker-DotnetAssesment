using InventoryTracker.Web.Data;
using InventoryTracker.Web.Models;
using InventoryTracker.Web.Services;
using InventoryTracker.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IStockService _stockService;

        public ProductsController(AppDbContext db, IStockService stockService)
        {
            _db = db;
            _stockService = stockService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Products.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Sku.Contains(search) || p.Name.Contains(search));
            }

            var products = await query.OrderBy(p => p.Name).ToListAsync();
            var stockLevels = await _stockService.GetStockLevelsAsync();

            var vm = products.Select(p => new ProductListItemVm
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                ReorderLevel = p.ReorderLevel,
                StockLevel = stockLevels.GetValueOrDefault(p.Id, 0)
            }).ToList();

            ViewData["Search"] = search;
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                .Include(p => p.Movements)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product is null)
            {
                return NotFound();
            }

            var vm = new ProductDetailVm
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Description = product.Description,
                ReorderLevel = product.ReorderLevel,
                StockLevel = await _stockService.GetStockLevelAsync(product.Id),
                Movements = product.Movements.OrderByDescending(m => m.CreatedUtc).ToList()
            };

            return View(vm);
        }

 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordMovement(RecordMovementVm vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["MovementError"] = "Please provide a valid quantity.";
                return RedirectToAction(nameof(Details), new { id = vm.ProductId });
            }

            var result = await _stockService.RecordMovementAsync(
                vm.ProductId, vm.Type, vm.Quantity, vm.Note);

            if (!result.IsSuccess)
            {
                TempData["MovementError"] = result.ErrorMessage;
            }

            return RedirectToAction(nameof(Details), new { id = vm.ProductId });
        }

   
        public IActionResult Create()
        {
            return View(new ProductFormVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormVm vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var skuExists = await _db.Products.AnyAsync(p => p.Sku == vm.Sku);
            if (skuExists)
            {
                ModelState.AddModelError(nameof(vm.Sku), "This SKU already exists.");
                return View(vm);
            }

            var product = new Product
            {
                Sku = vm.Sku,
                Name = vm.Name,
                Description = vm.Description,
                ReorderLevel = vm.ReorderLevel,
                IsActive = true,
                CreatedUtc = DateTime.UtcNow
            };

            try
            {
                _db.Products.Add(product);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(vm.Sku), "This SKU already exists.");
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }

     
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            var vm = new ProductFormVm
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Description = product.Description,
                ReorderLevel = product.ReorderLevel,
                IsActive = product.IsActive
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormVm vm)
        {
            if (id != vm.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var product = await _db.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            var skuTaken = await _db.Products
                .AnyAsync(p => p.Sku == vm.Sku && p.Id != id);

            if (skuTaken)
            {
                ModelState.AddModelError(nameof(vm.Sku), "This SKU already exists.");
                return View(vm);
            }

            product.Sku = vm.Sku;
            product.Name = vm.Name;
            product.Description = vm.Description;
            product.ReorderLevel = vm.ReorderLevel;
            product.IsActive = vm.IsActive;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(vm.Sku), "This SKU already exists.");
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}