namespace FinCtrl.Website.Controllers;

/// <summary>Страница списка.</summary>
/// <param name="Items">Записи страницы.</param>
/// <param name="TotalCount">Общее число записей по фильтру.</param>
public record PagedResponse<T>(List<T> Items, int TotalCount);
