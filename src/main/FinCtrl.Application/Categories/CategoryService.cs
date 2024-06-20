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

        public void Delete(params Category[] categories)
        {
            _categoryRepository.Delete(categories);
        }

        public Category GetById(int id)
        {
            return _categoryRepository.GetById(id);
        }

        public IEnumerable<Category> GetAll()
        {
            return _categoryRepository.GetAll();
        }

        public void Update(params Category[] categories)
        {
            foreach (var category in categories)
            {
                category.Updated();
            }

            _categoryRepository.Update(categories);
        }
    }
}
