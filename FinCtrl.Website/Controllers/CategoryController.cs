using FinCtrl.Application.Categories;
using FinCtrl.DAL.Implementation;
using FinCtrl.Infrastructure.Categories;
using Microsoft.AspNetCore.Mvc;

namespace FinCtrl.Website.Controllers
{
    public class CategoryController : Controller
    {
        ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [Route("category")]
        public IActionResult Index()
        {
            ViewData["items"] = _categoryService.GetAll();
            return View();
        }
    }
}
