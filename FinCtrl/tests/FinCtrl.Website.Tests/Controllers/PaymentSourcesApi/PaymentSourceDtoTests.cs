using FinCtrl.Domain;
using FinCtrl.Website.Controllers.PaymentSourcesApi;

namespace FinCtrl.Website.Tests.Controllers.PaymentSourcesApi;

public class PaymentSourceDtoTests
{
    [Fact]
    public void FromModel_WithCategory_MapsFieldsAndCategoryId()
    {
        var created = new DateTime(2025, 10, 1);
        var source = new PaymentSource(3, "Магазин", null, "у дома", created, null)
        {
            PaymentSourceCategory = new Category(7) { CategoryName = "Еда" }
        };

        var dto = PaymentSourceDto.FromModel(source);

        Assert.Equal((3, "Магазин", (int?)7, "у дома", created),
            (dto.PaymentSourceId, dto.PaymentSourceName, dto.CategoryId, dto.Description, dto.CreatedAt));
    }

    [Fact]
    public void FromModel_WithoutCategory_CategoryIdNull()
    {
        var dto = PaymentSourceDto.FromModel(new PaymentSource(3, "Магазин", null, null, new DateTime(2025, 10, 1), null));

        Assert.Null(dto.CategoryId);
        Assert.Null(dto.Description);
    }
}
