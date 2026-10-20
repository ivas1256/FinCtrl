namespace FinCtrl.Common.Application;

/// <summary>Некорректные входные данные или нарушение бизнес-правила (в API — 400).</summary>
public class ValidationException : Exception
{
    /// <summary>Создаёт исключение с сообщением для пользователя.</summary>
    public ValidationException(string message) : base(message)
    {
    }

    /// <summary>Создаёт исключение с сообщением для пользователя и исходной причиной (например, ошибкой разбора файла).</summary>
    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
