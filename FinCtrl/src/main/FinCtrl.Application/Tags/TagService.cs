using FinCtrl.Domain;

namespace FinCtrl.Application.Tags
{
    internal class TagService : ITagService
    {
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

        public async Task<Tag?> GetById(int tagId)
        {
            return await _repository.GetById(tagId);
        }

        public async Task<int> Create(Tag tag)
        {
            return await _repository.Create(tag);
        }

        public async Task Update(params Tag[] tags)
        {
            var now = _timeProvider.GetLocalNow().DateTime;
            foreach (var tag in tags)
            {
                tag.Updated(now);
            }

            await _repository.Update(tags);
        }

        public async Task Delete(params Tag[] tags)
        {
            await _repository.Delete(tags);
        }

        public async Task<bool> Exists(string tagName)
        {
            return await _repository.Exists(tagName);
        }
    }
}
