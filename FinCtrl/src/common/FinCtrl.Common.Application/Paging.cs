namespace FinCtrl.Common.Application;

/// <summary>Проверка параметров постраничной выборки.</summary>
public static class Paging
{
    /// <summary>Проверяет номер и размер страницы.</summary>
    /// <param name="page">Номер страницы с 1.</param>
    /// <param name="pageSize">Размер страницы; <c>null</c> — все записи.</param>
    /// <exception cref="ValidationException"><paramref name="page"/> или <paramref name="pageSize"/> меньше 1.</exception>
    public static void Validate(int page, int? pageSize)
    {
        if (page < 1)
            throw new ValidationException("Номер страницы должен быть не меньше 1");

        if (pageSize < 1)
            throw new ValidationException("Размер страницы должен быть не меньше 1");
    }
}
