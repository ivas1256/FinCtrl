using System.Text.Json;
using FinCtrl.Website.Controllers.PaymentsApi;

namespace FinCtrl.Website.Tests.Controllers.PaymentsApi;

public class EditPaymentRequestTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ToModel_EmptyBody_AllFieldsNull()
    {
        var request = JsonSerializer.Deserialize<EditPaymentRequest>("{}", WebOptions)!;

        var edit = request.ToModel(10);

        Assert.Equal(10, edit.PaymentId);
        Assert.Null(edit.CategoryId);
        Assert.Null(edit.AccountId);
        Assert.Null(edit.Description);
        Assert.Null(edit.ExcludeFromStats);
        Assert.Null(edit.TagIds);
    }

    [Fact]
    public void ToModel_BodyFromClient_IdFromRouteAndValuesFromBody()
    {
        const string body = """
            {"paymentId": 99, "categoryId": 7, "accountId": 0, "description": "", "excludeFromStats": true, "tagIds": []}
            """;
        var request = JsonSerializer.Deserialize<EditPaymentRequest>(body, WebOptions)!;

        var edit = request.ToModel(10);

        Assert.Equal(10, edit.PaymentId);
        Assert.Equal(7, edit.CategoryId);
        Assert.Equal(0, edit.AccountId);
        Assert.Equal("", edit.Description);
        Assert.True(edit.ExcludeFromStats);
        Assert.Empty(edit.TagIds!);
    }
}
