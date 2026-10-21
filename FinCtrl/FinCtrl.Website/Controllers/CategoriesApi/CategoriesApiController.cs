using System.ComponentModel.DataAnnotations;
using FinCtrl.Application.Categories;
using FinCtrl.Domain;
using Microsoft.AspNetCore.Mvc;

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

        /// <summary>Все категории плоским списком.</summary>
        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _categoryService.GetAll());
        }

        /// <summary>Дерево категорий.</summary>
        [HttpGet("getTree")]
        public async Task<IActionResult> GetTree()
        {
            return Ok(await _categoryService.GetCategoriesTree());
        }

        /// <summary>Создаёт категорию; 400 — нет названия, некорректный цвет или родитель не найден.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCrudDto dto)
        {
            var category = new Category
            {
                CategoryName = dto.CategoryName,
                ParentCategoryId = dto.ParentCategoryId,
                IsOnlyIncome = dto.IsOnlyIncome,
                OrderIndex = dto.OrderIndex,
                Color = dto.Color ?? string.Empty
            };
            var id = await _categoryService.Create(category);
            return Ok(new { categoryId = id });
        }

        /// <summary>Правит категорию; 404 — категория не найдена, 400 — некорректные данные или цикл в дереве.</summary>
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
                Color = dto.Color ?? string.Empty
            };
            await _categoryService.Update(category);
            return Ok();
        }

        /// <summary>Удаляет категорию; 404 — категория не найдена, 409 — у категории есть дочерние, источники или операции.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.Delete(id);
            return Ok();
        }
    }

    public class CategoryCrudDto
    {
        [Required(ErrorMessage = "Название категории обязательно")]
        public string CategoryName { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public bool IsOnlyIncome { get; set; }
        public int OrderIndex { get; set; }

        /// <summary>Цвет <c>#RRGGBB</c>; пусто — цвет по умолчанию.</summary>
        public string? Color { get; set; }
    }
}
