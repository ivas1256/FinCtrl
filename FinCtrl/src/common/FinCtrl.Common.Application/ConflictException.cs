namespace FinCtrl.Common.Application;

/// <summary>Операция конфликтует с текущим состоянием данных: дубликат имени, удаление используемой сущности (в API — 409).</summary>
public class ConflictException : Exception
{
    /// <summary>Создаёт исключение с сообщением для пользователя.</summary>
    public ConflictException(string message) : base(message)
    {
    }
}
