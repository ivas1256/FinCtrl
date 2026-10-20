using FinCtrl.Domain;

namespace FinCtrl.Application.Categories
{
    public interface ICategoryService
    {
        /// <summary>Категория по id.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Категория не найдена.</exception>
        Task<Category> GetById(int id);

        /// <summary>Все категории плоским списком.</summary>
        Task<IEnumerable<Category>> GetAll();

        /// <summary>Создаёт категорию; пустой цвет заменяется цветом по умолчанию.</summary>
        /// <returns>Id созданной категории.</returns>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Нет названия, некорректный цвет или родитель не найден.</exception>
        Task<int> Create(Category category);

        /// <summary>Сохраняет категорию; пустой цвет заменяется цветом по умолчанию.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Категория не найдена.</exception>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">
        /// Нет названия, некорректный цвет, родитель не найден или родитель — сама категория либо её потомок.
        /// </exception>
        Task Update(Category category);

        /// <summary>Удаляет категорию без связей.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Категория не найдена.</exception>
        /// <exception cref="FinCtrl.Common.Application.ConflictException">У категории есть дочерние категории, источники или операции.</exception>
        Task Delete(int id);

        /// <summary>Дерево категорий: корни с заполненными <c>Children</c>, по <c>OrderIndex</c>.</summary>
        Task<List<Category>> GetCategoriesTree();

        /// <summary>Все потомки категории в дереве (без неё самой); пустой список, если категории нет в дереве.</summary>
        List<Category> GetAllChildCategories(int parentCategoryId, List<Category> categoriesTree);
    }
}
