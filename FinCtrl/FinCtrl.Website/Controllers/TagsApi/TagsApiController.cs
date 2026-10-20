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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok((await _tagService.GetAll())
                .Select(TagDto.FromModel));
        }

        [HttpPost]
        public async Task<IActionResult> Create(TagDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TagName))
                return BadRequest("Название тега обязательно");

            if (await _tagService.Exists(dto.TagName))
                return BadRequest("Тег с таким названием уже существует");

            var newTag = Tag.CreateNew(dto.TagName, _timeProvider.GetLocalNow().DateTime);
            var id = await _tagService.Create(newTag);

            return Ok(TagDto.FromModel((await _tagService.GetById(id))!));
        }

        [HttpPut("{tagId}")]
        public async Task<IActionResult> Update(int tagId, TagDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TagName))
                return BadRequest("Название тега обязательно");

            var toUpdate = Tag.CreateFromExisting(tagId, dto.TagName);
            await _tagService.Update(toUpdate);

            return Ok();
        }

        [HttpDelete("{tagId}")]
        public async Task<IActionResult> Delete(int tagId)
        {
            var tag = await _tagService.GetById(tagId);
            if (tag is null)
                return NotFound();

            await _tagService.Delete(tag);

            return Ok();
        }
    }
}
