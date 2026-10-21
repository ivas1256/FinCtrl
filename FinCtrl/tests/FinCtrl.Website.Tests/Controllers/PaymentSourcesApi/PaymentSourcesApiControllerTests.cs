using FinCtrl.Application.PaymentSources;
using FinCtrl.Domain;
using FinCtrl.Website.Controllers;
using FinCtrl.Website.Controllers.PaymentSourcesApi;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace FinCtrl.Website.Tests.Controllers.PaymentSourcesApi;

public class PaymentSourcesApiControllerTests
{
    private readonly IPaymentSourceService _paymentSourceService = Substitute.For<IPaymentSourceService>();
    private readonly PaymentSourcesApiController _controller;

    public PaymentSourcesApiControllerTests()
    {
        _controller = new PaymentSourcesApiController(_paymentSourceService);
    }

    [Fact]
    public async Task EditSources_LegacyBody_EditsWithIdFromBody()
    {
        var dto = new PaymentSourceDto { PaymentSourceId = 42, PaymentSourceName = "Магазин", CategoryId = 7, Description = "у дома" };

        var result = await _controller.EditSources(dto);

        Assert.IsType<NoContentResult>(result);
        await _paymentSourceService.Received(1).Edit(42, "Магазин", 7, "у дома");
    }

    [Fact]
    public async Task EditSource_RouteId_EditsWithIdFromRoute()
    {
        var request = new EditPaymentSourceRequest { PaymentSourceName = "Магазин", CategoryId = null, Description = null };

        var result = await _controller.EditSource(42, request);

        Assert.IsType<NoContentResult>(result);
        await _paymentSourceService.Received(1).Edit(42, "Магазин", null, null);
    }

    [Fact]
    public async Task GetList_Filters_PassedAndPageMapped()
    {
        var source = new PaymentSource(3, "Магазин", 7, null, new DateTime(2025, 10, 1), null);
        _paymentSourceService.GetListPaged("маг", true, 2, 20).Returns((new List<PaymentSource> { source }, 21));

        var result = await _controller.GetList("маг", true, 2, 20);

        var page = Assert.IsType<PagedResponse<PaymentSourceDto>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(21, page.TotalCount);
        Assert.Equal(7, Assert.Single(page.Items).CategoryId);
    }

    [Fact]
    public async Task GetAll_Sources_MapsToDtos()
    {
        _paymentSourceService.GetAll().Returns(new List<PaymentSource> { new(3, "Магазин", null, null, new DateTime(2025, 10, 1), null) });

        var result = await _controller.GetAll();

        var dto = Assert.Single(Assert.IsType<List<PaymentSourceDto>>(Assert.IsType<OkObjectResult>(result).Value));
        Assert.Equal((3, "Магазин", (int?)null), (dto.PaymentSourceId, dto.PaymentSourceName, dto.CategoryId));
    }
}
