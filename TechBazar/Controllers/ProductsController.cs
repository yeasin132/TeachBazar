using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechBazar.Data;
using TechBazar.Models;
using TechBazar.Services;

namespace TechBazar.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMultiLanguageService _translationService;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger, UserManager<ApplicationUser> userManager, IMultiLanguageService translationService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _translationService = translationService;
        }

        [Authorize]
        public async Task<IActionResult> Index(int? categoryId, string searchString, string viewType = "grid")
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
                ViewBag.CategoryName = (await _context.Categories.FindAsync(categoryId.Value))?.Name;
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchString) ||
                    p.Description.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.ViewType = viewType;
            return View(await products.ToListAsync());
        }

        public async Task<IActionResult> Details(int id, int languageId = 1)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            // Sample usage of MultiLanguageService
            var translatedName = await _translationService.GetValueAsync("Product", "Name", product.Id, languageId);
            var translatedDescription = await _translationService.GetValueAsync("Product", "Description", product.Id, languageId);

            // Pass translated values to view
            ViewBag.TranslatedName = translatedName;
            ViewBag.TranslatedDescription = translatedDescription;
            ViewBag.LanguageId = languageId;

            // Get related products
            ViewBag.RelatedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .Take(4)
                .ToListAsync();

            return View(product);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            // Simplified cart implementation
            var cart = HttpContext.Session.GetString("Cart") ?? "{}";
            // In a real application, you'd deserialize, update, and serialize the cart
            TempData["SuccessMessage"] = "Product added to cart successfully!";
            return RedirectToAction("Details", new { id = productId });
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public async Task<IActionResult> AdminAdd()
        {
            // FIX: Load categories properly for dropdown
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [Authorize(Policy = "Products.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminAdd(Product product, IFormFile imageFile)
        {
            // Always get categories for the view
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");

            // Debug: Log received values
            _logger.LogInformation($"Received Product - Name: {product.Name}, Price: {product.Price}, Stock: {product.StockQuantity}, CategoryId: {product.CategoryId}");

            // Manual validation
            bool hasErrors = false;

            if (string.IsNullOrEmpty(product.Name))
            {
                ModelState.AddModelError("Name", "Product name is required");
                hasErrors = true;
            }

            if (string.IsNullOrEmpty(product.Description))
            {
                ModelState.AddModelError("Description", "Product description is required");
                hasErrors = true;
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Price must be greater than 0");
                hasErrors = true;
            }

            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError("StockQuantity", "Stock quantity cannot be negative");
                hasErrors = true;
            }

            if (product.CategoryId <= 0)
            {
                ModelState.AddModelError("CategoryId", "Please select a category");
                hasErrors = true;
            }

            if (hasErrors)
            {
                TempData["ErrorMessage"] = "Please fix the validation errors below.";
                return View(product);
            }

            try
            {
                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                    // Create directory if it doesn't exist
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    product.ImageUrl = "/images/" + uniqueFileName;
                }
                else if (string.IsNullOrEmpty(product.ImageUrl))
                {
                    // Set default image if no image uploaded and no URL provided
                    product.ImageUrl = "/images/default-product.jpg";
                }

                // Set additional properties
                product.CreatedDate = DateTime.Now;
                product.IsActive = true;

                // Add to database
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Product '{product.Name}' added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                TempData["ErrorMessage"] = $"An error occurred while adding the product: {ex.Message}";
                return View(product);
            }
        }

        [Authorize(Policy = "Products.Update")]
        [HttpGet]
        public async Task<IActionResult> AdminManage()
        {
            // FIX: Load categories for the dropdown in edit modal
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories; // THIS WAS MISSING

            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();

            return View(products);
        }

        [Authorize(Policy = "Products.Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction(nameof(AdminManage));
                }

                // Hard delete - permanently remove from database
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product removed successfully!";
                return RedirectToAction(nameof(AdminManage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product");
                TempData["ErrorMessage"] = "An error occurred while removing the product.";
                return RedirectToAction(nameof(AdminManage));
            }
        }

        [Authorize(Policy = "Products.Update")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(Product product)
        {
            try
            {
                // Manual validation instead of ModelState.IsValid
                if (string.IsNullOrEmpty(product.Name) ||
                    string.IsNullOrEmpty(product.Description) ||
                    product.Price <= 0 ||
                    product.StockQuantity < 0 ||
                    product.CategoryId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid product data. Please check all fields.";
                    return RedirectToAction(nameof(AdminManage));
                }

                var existingProduct = await _context.Products.FindAsync(product.Id);
                if (existingProduct == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction(nameof(AdminManage));
                }

                // Update properties
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.CategoryId = product.CategoryId;

                // Only update ImageUrl if provided
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    existingProduct.ImageUrl = product.ImageUrl;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Product '{product.Name}' updated successfully!";
                return RedirectToAction(nameof(AdminManage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                TempData["ErrorMessage"] = $"An error occurred while updating the product: {ex.Message}";
                return RedirectToAction(nameof(AdminManage));
            }
        }

        [Authorize(Policy = "Products.Update")]
        [HttpPost]
        public async Task<IActionResult> UpdateProductAjax([FromForm] Product product)
        {
            try
            {
                // Debug logging
                _logger.LogInformation($"UpdateProductAjax called with Id: {product.Id}, Name: {product.Name}, Price: {product.Price}, Stock: {product.StockQuantity}, CategoryId: {product.CategoryId}");

                // Manual validation
                if (string.IsNullOrEmpty(product.Name) ||
                    string.IsNullOrEmpty(product.Description) ||
                    product.Price <= 0 ||
                    product.StockQuantity < 0 ||
                    product.CategoryId <= 0)
                {
                    _logger.LogWarning("Validation failed for product update");
                    return Json(new { success = false, message = "Invalid product data. Please check all fields." });
                }

                // Check if category exists
                var category = await _context.Categories.FindAsync(product.CategoryId);
                if (category == null)
                {
                    _logger.LogWarning($"Category with Id {product.CategoryId} not found");
                    return Json(new { success = false, message = "Invalid category selected." });
                }

                var existingProduct = await _context.Products.FindAsync(product.Id);
                if (existingProduct == null)
                {
                    _logger.LogWarning($"Product with Id {product.Id} not found");
                    return Json(new { success = false, message = "Product not found." });
                }

                // Update properties
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.CategoryId = product.CategoryId;

                // Only update ImageUrl if provided
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    existingProduct.ImageUrl = product.ImageUrl;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Product {product.Name} updated successfully");
                return Json(new { success = true, message = $"Product '{product.Name}' updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product via AJAX");
                return Json(new { success = false, message = $"An error occurred while updating the product: {ex.Message}" });
            }
        }

        [Authorize(Policy = "Products.Update")]
        [HttpPost]
        public async Task<IActionResult> ToggleProductStatus(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                product.IsActive = !product.IsActive;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Product status updated successfully!", isActive = product.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling product status");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [Authorize(Policy = "Products.Update")]
        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                return Json(new
                {
                    success = true,
                    product = new
                    {
                        id = product.Id,
                        name = product.Name,
                        description = product.Description,
                        price = product.Price,
                        stockQuantity = product.StockQuantity,
                        categoryId = product.CategoryId,
                        imageUrl = product.ImageUrl,
                        isActive = product.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product");
                return Json(new { success = false, message = $"An error occurred while fetching the product: {ex.Message}" });
            }
        }

        [Authorize(Policy = "Products.Delete")]
        [HttpPost]
        public async Task<IActionResult> RemoveProductAjax(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // Hard delete - permanently remove from database
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Product removed successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product via AJAX");
                return Json(new { success = false, message = $"An error occurred while removing the product: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckPermission(string permission)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { hasPermission = false });
            }

            var user = await _userManager.GetUserAsync(User!);
            if (user == null)
            {
                return Json(new { hasPermission = false });
            }

            // Create a scope to get PermissionService
            using (var scope = HttpContext.RequestServices.CreateScope())
            {
                var permissionService = scope.ServiceProvider.GetRequiredService<PermissionService>();
                var hasPermission = await permissionService.HasPermissionAsync(user, permission);
                return Json(new { hasPermission = hasPermission });
            }
        }
    }
}