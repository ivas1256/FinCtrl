using FinCtrl.Common.Application;
using FinCtrl.Domain;

namespace FinCtrl.Application.Tags
{
    internal class TagService : ITagService
    {
        // Длина колонки TagName в БД (NVARCHAR(255))
        internal const int MaxNameLength = 255;

        private readonly ITagRepository _repository;
        private readonly TimeProvider _timeProvider;

        public TagService(ITagRepository repository, TimeProvider timeProvider)
        {
            _repository = repository;
            _timeProvider = timeProvider;
        }

        public async Task<List<Tag>> GetAll()
        {
            return await _repository.GetAll();
        }

        public async Task<Tag> GetById(int tagId)
        {
            return await _repository.GetById(tagId)
                ?? throw new NotFoundException($"Тег {tagId} не найден");
        }

        public async Task<int> Create(Tag tag)
        {
            ValidateName(tag);

            if (await _repository.Exists(tag.TagName))
                throw new ConflictException($"Тег «{tag.TagName}» уже существует");

            return await _repository.Create(tag);
        }

        public async Task Update(Tag tag)
        {
            ValidateName(tag);
            await GetById(tag.TagId);

            var sameName = await _repository.GetByName(tag.TagName);
            if (sameName is not null && sameName.TagId != tag.TagId)
                throw new ConflictException($"Тег «{tag.TagName}» уже существует");

            tag.Updated(_timeProvider.GetLocalNow().DateTime);
            await _repository.Update(tag);
        }

        public async Task Delete(int tagId)
        {
            var tag = await GetById(tagId);

            await _repository.Delete(tag);
        }

        private static void ValidateName(Tag tag)
        {
            if (string.IsNullOrWhiteSpace(tag.TagName))
                throw new ValidationException("Название тега обязательно");

            if (tag.TagName.Length > MaxNameLength)
                throw new ValidationException($"Название тега должно быть не длиннее {MaxNameLength} символов");
        }
    }
}
