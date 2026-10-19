using FinCtrl.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinCtrl.Application.Tags
{
    public interface ITagService
    {
        Task<List<Tag>> GetAll();
        Task<Tag?> GetById(int tagId);
        Task<Tag?> GetByName(string tagName);

        Task<int> Create(Tag tag);
        Task Update(params Tag[] tags);
        Task Delete(params Tag[] tags);

        Task<bool> Exists(string tagName);
    }
}
