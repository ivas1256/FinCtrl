using FinCtrl.Domain;

namespace FinCtrl.Application.Categories;

/// <summary>Хранилище категорий.</summary>
public interface ICategoryRepository
{
    /// <summary>Категория по id или <c>null</c>, если не найдена.</summary>
    Task<Category?> GetById(int id);

    /// <summary>Все категории плоским списком, без заполненных <c>Children</c>.</summary>
    Task<IEnumerable<Category>> GetAll();

    /// <summary>Создаёт категорию.</summary>
    /// <returns>Id созданной категории.</returns>
    Task<int> Create(Category category);

    /// <summary>Сохраняет изменения категорий.</summary>
    Task Update(params Category[] categories);

    /// <summary>Удаляет категории.</summary>
    Task Delete(params Category[] categories);
}
