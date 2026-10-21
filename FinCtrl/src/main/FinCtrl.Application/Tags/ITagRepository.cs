using FinCtrl.Domain;

namespace FinCtrl.Application.Tags;

/// <summary>Хранилище тегов.</summary>
public interface ITagRepository
{
    /// <summary>Все теги по алфавиту.</summary>
    Task<List<Tag>> GetAll();

    /// <summary>Тег по id или <c>null</c>, если не найден.</summary>
    Task<Tag?> GetById(int tagId);

    /// <summary>Тег по имени или <c>null</c>, если не найден.</summary>
    Task<Tag?> GetByName(string tagName);

    /// <summary>Создаёт тег.</summary>
    /// <returns>Id созданного тега.</returns>
    Task<int> Create(Tag tag);

    /// <summary>Сохраняет изменения тегов.</summary>
    Task Update(params Tag[] tags);

    /// <summary>Удаляет теги вместе с их связями с платежами.</summary>
    Task Delete(params Tag[] tags);

    /// <summary>Есть ли тег с таким именем.</summary>
    Task<bool> Exists(string tagName);
}
