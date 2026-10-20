using System.ComponentModel.DataAnnotations;

namespace FinCtrl.Website.Controllers.PaymentsApi;

/// <summary>Тело запроса массового назначения тегов платежам периода по категориям.</summary>
public class BulkAssignTagsRequest
{
    /// <summary>Первый день периода.</summary>
    public DateTime From { get; set; }

    /// <summary>Последний день периода (включительно).</summary>
    public DateTime To { get; set; }

    [MinLength(1, ErrorMessage = "Выберите хотя бы одну категорию")]
    public List<int> CategoryIds { get; set; } = new();

    [MinLength(1, ErrorMessage = "Выберите хотя бы один тег")]
    public List<int> TagIds { get; set; } = new();
}
