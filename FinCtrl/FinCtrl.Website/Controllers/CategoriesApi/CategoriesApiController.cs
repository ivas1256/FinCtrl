using FinCtrl.Application.Categories;
using FinCtrl.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FinCtrl.Website.Controllers.CategoriesApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesApiController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesApiController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _categoryService.GetAll());
        }

        [HttpGet("getTree")]
        public async Task<IActionResult> GetTree()
        {
            return Ok(await _categoryService.GetCategoriesTree());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCrudDto dto)
        {
            var category = new Category
            {
                CategoryName = dto.CategoryName,
                ParentCategoryId = dto.ParentCategoryId,
                IsOnlyIncome = dto.IsOnlyIncome,
                OrderIndex = dto.OrderIndex,
                Color = string.IsNullOrWhiteSpace(dto.Color) ? Category.DefaultColor : dto.Color
            };
            var id = await _categoryService.Create(category);
            return Ok(new { categoryId = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryCrudDto dto)
        {
            var category = new Category
            {
                CategoryId = id,
                CategoryName = dto.CategoryName,
                ParentCategoryId = dto.ParentCategoryId,
                IsOnlyIncome = dto.IsOnlyIncome,
                OrderIndex = dto.OrderIndex,
                Color = string.IsNullOrWhiteSpace(dto.Color) ? Category.DefaultColor : dto.Color
            };
            await _categoryService.Update(category);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryService.GetById(id);
            if (category == null) return NotFound();
            await _categoryService.Delete(category);
            return Ok();
        }
    }

    public class CategoryCrudDto
    {
        public string CategoryName { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsOnlyIncome { get; set; }
        public int OrderIndex { get; set; }
        public string Color { get; set; }
    }
}
