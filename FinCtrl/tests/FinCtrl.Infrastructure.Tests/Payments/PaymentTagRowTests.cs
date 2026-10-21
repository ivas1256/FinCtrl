using FinCtrl.Infrastructure.Payments;

namespace FinCtrl.Infrastructure.Tests.Payments;

public class PaymentTagRowTests
{
    [Fact]
    public void MapToTag_Row_MapsTagFields()
    {
        var created = new DateTime(2025, 10, 1);
        var row = new PaymentTagRow { PaymentId = 10, TagId = 3, TagName = "Поездка", CreatedAt = created, LastUpdatedAt = null };

        var tag = row.MapToTag();

        Assert.Equal((3, "Поездка", created, (DateTime?)null), (tag.TagId, tag.TagName, tag.CreatedAt, tag.LastUpdatedAt));
    }
}
