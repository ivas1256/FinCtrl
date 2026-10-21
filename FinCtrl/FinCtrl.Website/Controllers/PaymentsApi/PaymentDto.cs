using FinCtrl.Domain;
using FinCtrl.Website.Controllers.AccountsApi;
using FinCtrl.Website.Controllers.CategoriesApi;
using FinCtrl.Website.Controllers.PaymentSourcesApi;
using FinCtrl.Website.Controllers.TagsApi;

namespace FinCtrl.Website.Controllers.PaymentsApi;

/// <summary>Платёж — единый формат для таблицы главной, списка операций и платежей источника.</summary>
public class PaymentDto
{
    /// <summary>Значение <see cref="CategorySource"/>: категория задана у самого платежа.</summary>
    public const string CategorySourcePayment = "payment";

    /// <summary>Значение <see cref="CategorySource"/>: категория взята у источника платежа.</summary>
    public const string CategorySourceSource = "source";

    public int PaymentId { get; set; }
    public PaymentTypeEnum PaymentType { get; set; }
    public decimal PaymentSum { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentSourceDto? PaymentSource { get; set; }
    public AccountDto? Account { get; set; }

    /// <summary>Своя категория платежа (без учёта источника).</summary>
    public CategoryDto? Category { get; set; }

    /// <summary>Итоговая категория: своя или, если её нет, категория источника.</summary>
    public CategoryDto? EffectiveCategory { get; set; }

    /// <summary>Откуда итоговая категория: <c>payment</c>, <c>source</c> или <c>null</c> (категории нет).</summary>
    public string? CategorySource { get; set; }

    public string? PaymentDescription { get; set; }
    public bool ExcludeFromStats { get; set; }
    public List<TagDto> Tags { get; set; } = new();

    public static PaymentDto FromModel(Payment payment)
    {
        var effectiveCategory = payment.CalculatedCategory;

        return new PaymentDto
        {
            PaymentId = payment.PaymentId,
            PaymentType = payment.PaymentType,
            PaymentSum = payment.PaymentSum,
            PaymentDate = payment.PaymentDate,
            PaymentSource = payment.PaymentSource is null ? null : PaymentSourceDto.FromModel(payment.PaymentSource),
            Account = payment.Account is null ? null : AccountDto.FromModel(payment.Account),
            Category = payment.PaymentCategory is null ? null : CategoryDto.FromModel(payment.PaymentCategory),
            EffectiveCategory = effectiveCategory is null ? null : CategoryDto.FromModel(effectiveCategory),
            CategorySource = payment.PaymentCategory is not null ? CategorySourcePayment
                : effectiveCategory is not null ? CategorySourceSource
                : null,
            PaymentDescription = payment.PaymentDescription,
            ExcludeFromStats = payment.ExcludeFromStats,
            Tags = payment.Tags.Select(TagDto.FromModel).ToList()
        };
    }
}
