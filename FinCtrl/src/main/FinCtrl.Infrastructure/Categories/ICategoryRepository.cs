using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.Categories
{
    public interface ICategoryRepository
    {
        Task<Category?> GetById(int id);

        Task<IEnumerable<Category>> GetAll();

        Task<int> Create(Category category);

        Task Update(params Category[] categories);

        Task Delete(params Category[] categories);
        Task<List<Category>> GetCategoriesTree();
    }
}
