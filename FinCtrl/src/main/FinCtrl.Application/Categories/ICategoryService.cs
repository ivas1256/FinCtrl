using FinCtrl.Domain;

namespace FinCtrl.Application.Categories
{
    public interface ICategoryService
    {
        Task<Category?> GetById(int id);

        Task<IEnumerable<Category>> GetAll();

        Task<int> Create(Category category);

        Task Update(params Category[] categories);

        Task Delete(params Category[] categories);
        Task<List<Category>> GetCategoriesTree();
        List<Category> GetAllChildCategories(int parentCategoryId, List<Category> categoriesTree);
    }
}
