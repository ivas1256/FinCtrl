using System.Text.RegularExpressions;
using FinCtrl.Common.Application;
using FinCtrl.Domain;

namespace FinCtrl.Application.Categories
{
    internal class CategoryService : ICategoryService
    {
        // Колонка Color — NVARCHAR(7), фронтенд передаёт значение <input type="color">
        private static readonly Regex ColorRegex = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

        private readonly ICategoryRepository _categoryRepository;
        private readonly TimeProvider _timeProvider;

        public CategoryService(ICategoryRepository categoryRepository, TimeProvider timeProvider)
        {
            _categoryRepository = categoryRepository;
            _timeProvider = timeProvider;
        }

        public async Task Delete(int id)
        {
            var category = await GetById(id);

            var usage = await _categoryRepository.GetUsage(id);
            if (usage.IsUsed)
            {
                throw new ConflictException(
                    $"Категорию «{category.CategoryName}» нельзя удалить: дочерних категорий — {usage.ChildCount}, "
                    + $"источников — {usage.PaymentSourceCount}, операций — {usage.PaymentCount}. Сначала перенесите их в другую категорию.");
            }

            await _categoryRepository.Delete(category);
        }

        public async Task<Category> GetById(int id)
        {
            return await _categoryRepository.GetById(id)
                ?? throw new NotFoundException($"Категория {id} не найдена");
        }

        public async Task<IEnumerable<Category>> GetAll()
        {
            return await _categoryRepository.GetAll();
        }

        public async Task<int> Create(Category category)
        {
            NormalizeAndValidate(category);
            if (category.ParentCategoryId is int parentId)
            {
                await EnsureParentExists(parentId);
            }

            category.CreatedAt = _timeProvider.GetLocalNow().DateTime;
            return await _categoryRepository.Create(category);
        }

        public async Task Update(Category category)
        {
            NormalizeAndValidate(category);
            await GetById(category.CategoryId);
            if (category.ParentCategoryId is int parentId)
            {
                await EnsureValidParent(category.CategoryId, parentId);
            }

            category.Updated(_timeProvider.GetLocalNow().DateTime);
            await _categoryRepository.Update(category);
        }

        private static void NormalizeAndValidate(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.CategoryName))
                throw new ValidationException("Название категории обязательно");

            if (string.IsNullOrWhiteSpace(category.Color))
            {
                category.Color = Category.DefaultColor;
            }
            else if (!ColorRegex.IsMatch(category.Color))
            {
                throw new ValidationException($"Некорректный цвет «{category.Color}»: ожидается формат #RRGGBB");
            }
        }

        private async Task EnsureParentExists(int parentId)
        {
            if (await _categoryRepository.GetById(parentId) is null)
                throw new ValidationException($"Родительская категория {parentId} не найдена");
        }

        // Родитель не может быть самой категорией или её потомком — иначе в дереве появится цикл,
        // и ветка пропадёт из GetCategoriesTree (недостижима от корней).
        private async Task EnsureValidParent(int categoryId, int parentId)
        {
            var parentById = (await _categoryRepository.GetAll()).ToDictionary(c => c.CategoryId, c => c.ParentCategoryId);
            if (!parentById.ContainsKey(parentId))
                throw new ValidationException($"Родительская категория {parentId} не найдена");

            var visited = new HashSet<int>();
            for (int? current = parentId; current is int id && visited.Add(id); current = parentById.GetValueOrDefault(id))
            {
                if (id == categoryId)
                    throw new ValidationException("Родительская категория не может быть самой категорией или её дочерней категорией");
            }
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
