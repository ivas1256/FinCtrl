namespace FinCtrl.Application.Categories;

/// <summary>Что ссылается на категорию: без этих связей её можно удалить.</summary>
/// <param name="ChildCount">Дочерние категории.</param>
/// <param name="PaymentSourceCount">Источники платежа с этой категорией.</param>
/// <param name="PaymentCount">Операции с этой собственной категорией, включая удалённые.</param>
public record CategoryUsage(int ChildCount, int PaymentSourceCount, int PaymentCount)
{
    /// <summary>Есть ли хотя бы одна связь.</summary>
    public bool IsUsed => ChildCount > 0 || PaymentSourceCount > 0 || PaymentCount > 0;
}
