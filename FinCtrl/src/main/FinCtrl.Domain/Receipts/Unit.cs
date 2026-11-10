namespace FinCtrl.Domain.Receipts;

/// <summary>Единица измерения позиции чека (шт, кг, л и т.п.).</summary>
public class Unit
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}
