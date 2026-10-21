using FinCtrl.Domain;

namespace FinCtrl.Application.Tags
{
    public interface ITagService
    {
        /// <summary>Все теги по алфавиту.</summary>
        Task<List<Tag>> GetAll();

        /// <summary>Тег по id.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Тег не найден.</exception>
        Task<Tag> GetById(int tagId);

        /// <summary>Создаёт тег.</summary>
        /// <returns>Id созданного тега.</returns>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Название не заполнено или слишком длинное.</exception>
        /// <exception cref="FinCtrl.Common.Application.ConflictException">Тег с таким названием уже есть.</exception>
        Task<int> Create(Tag tag);

        /// <summary>Сохраняет название тега.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Тег не найден.</exception>
        /// <exception cref="FinCtrl.Common.Application.ValidationException">Название не заполнено или слишком длинное.</exception>
        /// <exception cref="FinCtrl.Common.Application.ConflictException">Название занято другим тегом.</exception>
        Task Update(Tag tag);

        /// <summary>Удаляет тег вместе с его связями с операциями.</summary>
        /// <exception cref="FinCtrl.Common.Application.NotFoundException">Тег не найден.</exception>
        Task Delete(int tagId);
    }
}
