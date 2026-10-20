using FinCtrl.Domain.Statistic;

namespace FinCtrl.Website.Controllers.PaymentsApi;

/// <summary>Ячейка таблицы главной: сумма и платежи категории (с потомками) за период.</summary>
public class PeriodDto
{
    public decimal TotalSum { get; set; }
    public List<PaymentDto> Payments { get; set; } = new();

    public static PeriodDto FromModel(PaymentsTableCell cell) => new()
    {
        TotalSum = cell.TotalSum,
        Payments = cell.Payments.Select(PaymentDto.FromModel).ToList()
    };
}
