using FinCtrl.Application.Categories;
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
        [ProducesResponseType(typeof(List<CategoryDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            return Ok((await _categoryService.GetAll()).Select(CategoryDto.FromModel).ToList());
        }

        /// <summary>Дерево категорий.</summary>
        [HttpGet("getTree")]
        [ProducesResponseType(typeof(List<CategoryDto>), 200)]
        public async Task<IActionResult> GetTree()
        {
            return Ok((await _categoryService.GetCategoriesTree()).Select(CategoryDto.FromModel).ToList());
        }

        /// <summary>Создаёт категорию; 400 — нет названия, некорректный цвет или родитель не найден.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(CreateCategoryResponse), 200)]
        public async Task<IActionResult> Create([FromBody] SaveCategoryRequest request)
        {
            var id = await _categoryService.Create(request.ToModel());
            return Ok(new CreateCategoryResponse(id));
        }

        /// <summary>Правит категорию; 404 — категория не найдена, 400 — некорректные данные или цикл в дереве.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveCategoryRequest request)
        {
            await _categoryService.Update(request.ToModel(id));
            return NoContent();
        }

        /// <summary>Удаляет категорию; 404 — категория не найдена, 409 — у категории есть дочерние, источники или операции.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.Delete(id);
            return NoContent();
        }
    }
}
