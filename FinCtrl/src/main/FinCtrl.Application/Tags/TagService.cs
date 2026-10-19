using FinCtrl.Domain;
using FinCtrl.Infrastructure.Tags;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinCtrl.Application.Tags
{
    internal class TagService : ITagService
    {
        private readonly ITagRepository _repository;

        public TagService(ITagRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Tag>> GetAll()
        {
            return await _repository.GetAll();
        }

        public async Task<Tag?> GetById(int tagId)
        {
            return await _repository.GetById(tagId);
        }

        public async Task<Tag?> GetByName(string tagName)
        {
            return await _repository.GetByName(tagName);
        }

        public async Task<int> Create(Tag tag)
        {
            return await _repository.Create(tag);
        }

        public async Task Update(params Tag[] tags)
        {
            foreach (var tag in tags)
            {
                tag.Updated();
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
