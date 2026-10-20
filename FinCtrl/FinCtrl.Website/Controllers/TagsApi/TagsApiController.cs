using FinCtrl.Application.Tags;
using FinCtrl.Domain;
using FinCtrl.Website.RequestDto;
using Microsoft.AspNetCore.Mvc;

namespace FinCtrl.Website.Controllers.TagsApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsApiController : ControllerBase
    {
        private readonly ITagService _tagService;
        private readonly TimeProvider _timeProvider;

        public TagsApiController(ITagService tagService, TimeProvider timeProvider)
        {
            _tagService = tagService;
            _timeProvider = timeProvider;
        }

        /// <summary>Все теги по алфавиту.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok((await _tagService.GetAll())
                .Select(TagDto.FromModel));
        }

        /// <summary>Создаёт тег; 409 — тег с таким названием уже есть.</summary>
        [HttpPost]
        public async Task<IActionResult> Create(TagDto dto)
        {
            var newTag = Tag.CreateNew(dto.TagName, _timeProvider.GetLocalNow().DateTime);
            var id = await _tagService.Create(newTag);

            return Ok(TagDto.FromModel(await _tagService.GetById(id)));
        }

        /// <summary>Переименовывает тег; 404 — тег не найден, 409 — название занято.</summary>
        [HttpPut("{tagId}")]
        public async Task<IActionResult> Update(int tagId, TagDto dto)
        {
            var toUpdate = Tag.CreateFromExisting(tagId, dto.TagName);
            await _tagService.Update(toUpdate);

            return Ok();
        }

        /// <summary>Удаляет тег вместе со связями с операциями; 404 — тег не найден.</summary>
        [HttpDelete("{tagId}")]
        public async Task<IActionResult> Delete(int tagId)
        {
            await _tagService.Delete(tagId);

            return Ok();
        }
    }
}
