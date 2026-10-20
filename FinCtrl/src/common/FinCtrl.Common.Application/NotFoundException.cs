namespace FinCtrl.Common.Application;

/// <summary>Запрошенная сущность не найдена (в API — 404).</summary>
public class NotFoundException : Exception
{
    /// <summary>Создаёт исключение с сообщением для пользователя.</summary>
    public NotFoundException(string message) : base(message)
    {
    }
}
