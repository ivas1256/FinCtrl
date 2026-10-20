using FinCtrl.Domain;

namespace FinCtrl.Application.Categories
{
    internal class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly TimeProvider _timeProvider;

        public CategoryService(ICategoryRepository categoryRepository, TimeProvider timeProvider)
        {
            _categoryRepository = categoryRepository;
            _timeProvider = timeProvider;
        }

        public async Task Delete(params Category[] categories)
        {
            await _categoryRepository.Delete(categories);
        }

        public async Task<Category?> GetById(int id)
        {
            return await _categoryRepository.GetById(id);
        }

        public async Task<IEnumerable<Category>> GetAll()
        {
            return await _categoryRepository.GetAll();
        }

        public async Task<int> Create(Category category)
        {
            category.CreatedAt = _timeProvider.GetLocalNow().DateTime;
            return await _categoryRepository.Create(category);
        }

        public async Task Update(params Category[] categories)
        {
            var now = _timeProvider.GetLocalNow().DateTime;
            foreach (var category in categories)
            {
                category.Updated(now);
            }

            await _categoryRepository.Update(categories);
        }

        // Дерево собирается в памяти из одного SELECT'а (раньше — запрос на каждый узел).
        // Категории с несуществующим родителем (и циклы) недостижимы от корней и в дерево не попадают.
        public async Task<List<Category>> GetCategoriesTree()
        {
            var categories = (await _categoryRepository.GetAll())
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.CategoryId)
                .ToList();

            var childrenByParentId = categories
                .Where(c => c.ParentCategoryId.HasValue)
                .ToLookup(c => c.ParentCategoryId!.Value);

            foreach (var category in categories)
            {
                category.Children = childrenByParentId[category.CategoryId].ToList();
            }

            return categories.Where(c => c.ParentCategoryId is null).ToList();
        }

        public List<Category> GetAllChildCategories(int parentCategoryId, List<Category> categoriesTree)
        {
            var targetCategory = FindCategoryWithChildren(categoriesTree, parentCategoryId);
            return targetCategory == null
                ? new List<Category>()
                : FlattenCategoryHierarchy(targetCategory);
        }

        // Рекурсивный поиск категории в дереве по ID
        private Category? FindCategoryWithChildren(List<Category> categories, int categoryId)
        {
            foreach (var category in categories)
            {
                if (category.CategoryId == categoryId) return category;

                var found = FindCategoryWithChildren(category.Children, categoryId);
                if (found != null) return found;
            }
            return null;
        }

        // Сбор всех дочерних категорий через BFS
        private List<Category> FlattenCategoryHierarchy(Category root)
        {
            var result = new List<Category>();
            var queue = new Queue<Category>(root.Children);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                foreach (var child in current.Children)
                {
                    queue.Enqueue(child);
                }
            }

            return result;
        }
    }
}
