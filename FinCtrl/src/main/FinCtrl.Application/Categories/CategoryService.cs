using FinCtrl.Domain;
using FinCtrl.Infrastructure.Categories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Application.Categories
{
    internal class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task Delete(params Category[] categories)
        {
            await _categoryRepository.Delete(categories);
        }

        public async Task<Category> GetById(int id)
        {
            return await _categoryRepository.GetById(id);
        }

        public async Task<IEnumerable<Category>> GetAll()
        {
            return await _categoryRepository.GetAll();
        }

        public async Task<int> Create(Category category)
        {
            return await _categoryRepository.Create(category);
        }

        public async Task Update(params Category[] categories)
        {
            foreach (var category in categories)
            {
                category.Updated();
            }

            await _categoryRepository.Update(categories);
        }

        public async Task<List<Category>> GetCategoriesTree()
        {
            return await _categoryRepository.GetCategoriesTree();
        }

        public List<Category> GetAllChildCategories(int parentCategoryId, List<Category> categoriesTree)
        {
            var targetCategory = FindCategoryWithChildren(categoriesTree, parentCategoryId);
            return targetCategory == null
                ? new List<Category>()
                : FlattenCategoryHierarchy(targetCategory);
        }

        // Рекурсивный поиск категории в дереве по ID
        private Category FindCategoryWithChildren(List<Category> categories, int categoryId)
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
