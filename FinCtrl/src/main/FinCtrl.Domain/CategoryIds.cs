namespace FinCtrl.Domain;

/// <summary>Служебные id категорий.</summary>
public static class CategoryIds
{
    /// <summary>Категория «Накопления» в БД: переводы на накопления — не траты, в итоги не входят.</summary>
    public const int Savings = 33;

    /// <summary>Фейковая категория «Без категории» (только в отчётах, в БД её нет).</summary>
    public const int FakeEmpty = int.MaxValue - 1;

    /// <summary>Фейковая итоговая строка «ВСЕГО потрачено» (только в отчётах, в БД её нет).</summary>
    public const int FakeTotal = int.MaxValue;
}
