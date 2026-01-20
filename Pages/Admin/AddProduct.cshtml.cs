using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechBazar.Data;
using TechBazar.Models;

namespace TechBazar.Pages.Admin
{
    public class AddProductModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddProductModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product Product { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Products.Add(Product);
            var saved = await _context.SaveChangesAsync();
            if (saved > 0)
            {
                TempData["SuccessMessage"] = "Product added.";
                return RedirectToPage("/Products/Index"); // adjust target page
            }

            ModelState.AddModelError(string.Empty, "Unable to save product.");
            return Page();
        }
    }
}
