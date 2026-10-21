namespace FinCtrl.Website.Controllers.PaymentsApi;

/// <summary>Ответ массового назначения тегов.</summary>
/// <param name="AffectedCount">Число платежей, подошедших под период и категории.</param>
public record BulkAssignTagsResponse(int AffectedCount);
