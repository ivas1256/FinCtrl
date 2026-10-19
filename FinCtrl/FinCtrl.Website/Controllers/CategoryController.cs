using FinCtrl.Application.Categories;
using FinCtrl.Domain;
using FinCtrl.Infrastructure.Categories;
using FinCtrl.Website.RequestDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinCtrl.Website.Controllers
{
    [Route("[controller]")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet, Route("all")]
        public async Task<IActionResult> Index()
        {
            var list = ConvertToJsObj(await _categoryService.GetCategoriesTree());

            var jso = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
            ViewData["tree"] = JsonSerializer.Serialize(list, jso);


            var allCategories = new List<Category>
            {
                Category.CreateFakeEmpty()
            };
            allCategories.AddRange((await _categoryService.GetAll()).OrderBy(x => x.CategoryName));

            ViewData["allCategories"] = allCategories.Select(x => new SelectListItem
            {
                Text = x.CategoryName,
                Value = x.CategoryId.ToString()
            });
            ViewData["items"] = await _categoryService.GetAll();
            return View();
        }

        [HttpPost, Route("update")]
        public async Task<IActionResult> Update(CategoryDto category)
        {
            await _categoryService.Update(category.ToModel());

            return Redirect("all");
        }

        private List<object> ConvertToJsObj(List<Category> categories)
        {
            var result = new List<object>();
            foreach (var category in categories)
            {
                result.Add(Convert(category));
            }

            return result;
        }

        private object Convert(Category category)
        {
            if (category.Children.Any() is false)
            {
                return new
                {
                    id = category.CategoryId,
                    name = category.CategoryName,
                    isIncome = category.IsOnlyIncome,
                    sortIndex = category.OrderIndex,
                    parentCategoryId = category.ParentCategoryId
                };
            }

            var items = new List<object>();
            foreach (var child in category.Children)
            {
                items.Add(Convert(child));
            }

            return new
            {
                id = category.CategoryId,
                name = category.CategoryName,
                isIncome = category.IsOnlyIncome,
                sortIndex = category.OrderIndex,
                parentCategoryId = category.ParentCategoryId,
                subcategories = items
            };
        }
    }
}
