using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport;
using FinCtrl.Application.Statistic;
using FinCtrl.Common;
using FinCtrl.Domain;
using FinCtrl.Domain.Statistic;
using FinCtrl.Website.Controllers;
using FinCtrl.Website.Controllers.PaymentsApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace FinCtrl.Website.Tests.Controllers.PaymentsApi;

public class PaymentsApiControllerTests
{
    private readonly IMainStatisticService _mainStatisticService = Substitute.For<IMainStatisticService>();
    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly IPaymentsImportService _paymentsImportService = Substitute.For<IPaymentsImportService>();
    private readonly PaymentsApiController _controller;

    public PaymentsApiControllerTests()
    {
        _mainStatisticService.GetPaymentsTable(Arg.Any<DateRange?>(), Arg.Any<GroupingType>(), Arg.Any<int[]?>())
            .Returns(new PaymentsTable());
        _paymentService.GetListPaged(default, default, default, default, default!, default!, default, default, default)
            .ReturnsForAnyArgs((new List<Payment>(), 0));
        _controller = new PaymentsApiController(_mainStatisticService, _paymentService, _paymentsImportService);
    }

    [Fact]
    public async Task Get_BothDates_PassesInclusiveRangeGroupingAndTags()
    {
        int[] tagIds = [1, 2];

        var result = await _controller.Get(new DateTime(2025, 10, 1, 15, 0, 0), new DateTime(2025, 10, 31), GroupingType.Week, tagIds);

        Assert.IsType<GetPaymentsTableResponse>(Assert.IsType<OkObjectResult>(result).Value);
        await _mainStatisticService.Received(1).GetPaymentsTable(
            new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1)), GroupingType.Week, tagIds);
    }

    [Theory]
    [InlineData("2025-10-01", null)]
    [InlineData(null, "2025-10-31")]
    [InlineData(null, null)]
    public async Task Get_NotBothDates_PassesNullRangeForDefault(string? from, string? to)
    {
        await _controller.Get(ParseDate(from), ParseDate(to), GroupingType.Month);

        await _mainStatisticService.Received(1).GetPaymentsTable(null, GroupingType.Month, null);
    }

    [Fact]
    public async Task Get_Table_MapsToResponse()
    {
        var period = new PaymentsTablePeriod { Key = "102025", Range = new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1)) };
        _mainStatisticService.GetPaymentsTable(Arg.Any<DateRange?>(), Arg.Any<GroupingType>(), Arg.Any<int[]?>())
            .Returns(new PaymentsTable
            {
                Periods = [period],
                Tree = [new PaymentsTableNode { Category = new Category(1) { CategoryName = "Еда" } }]
            });

        var result = await _controller.Get(null, null, GroupingType.Month);

        var response = Assert.IsType<GetPaymentsTableResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Еда", Assert.Single(response.PaymentsTree).CategoryName);
        Assert.Equal(new DateTime(2025, 11, 1), response.Periods["102025"].To);
    }

    [Fact]
    public async Task EditPayment_RouteId_EditsPaymentWithRouteIdAndReturnsNoContent()
    {
        var request = new EditPaymentRequest { AccountId = 0, Description = "Обед", TagIds = [1] };

        var result = await _controller.EditPayment(10, request);

        Assert.IsType<NoContentResult>(result);
        await _paymentService.Received(1).Edit(Arg.Is<PaymentEdit>(e =>
            e.PaymentId == 10 && e.AccountId == 0 && e.Description == "Обед" && e.CategoryId == null && e.TagIds!.SequenceEqual(new[] { 1 })));
    }

    [Fact]
    public async Task BulkAssignTags_InclusiveDates_PassesRangeAndReturnsAffectedCount()
    {
        _paymentService.BulkAssignTags(Arg.Any<DateRange>(), Arg.Any<List<int>>(), Arg.Any<List<int>>()).Returns(4);
        var request = new BulkAssignTagsRequest
        {
            From = new DateTime(2025, 10, 1),
            To = new DateTime(2025, 10, 31),
            CategoryIds = [7],
            TagIds = [1, 2]
        };

        var result = await _controller.BulkAssignTags(request);

        var response = Assert.IsType<BulkAssignTagsResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(4, response.AffectedCount);
        await _paymentService.Received(1).BulkAssignTags(
            new DateRange(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1)), request.CategoryIds, request.TagIds);
    }

    [Fact]
    public async Task GetList_Dates_FromIsDayStartAndToIsNextDay()
    {
        int[] tagIds = [3];

        await _controller.GetList(new DateTime(2025, 10, 1, 15, 0, 0), new DateTime(2025, 10, 31, 9, 0, 0),
            sourceId: 5, noCategoryOnly: true, sortBy: "source", sortDir: "asc", page: 2, pageSize: 50, tagIds: tagIds);

        await _paymentService.Received(1).GetListPaged(
            new DateTime(2025, 10, 1), new DateTime(2025, 11, 1), 5, true, "source", "asc", 2, 50, tagIds);
    }

    [Fact]
    public async Task GetList_NoDates_PassesNullsAndDefaults()
    {
        await _controller.GetList(null, null);

        await _paymentService.Received(1).GetListPaged(null, null, null, false, "date", "desc", 1, null, null);
    }

    [Fact]
    public async Task GetList_Page_MapsPaymentsAndTotalCount()
    {
        _paymentService.GetListPaged(default, default, default, default, default!, default!, default, default, default)
            .ReturnsForAnyArgs((new List<Payment> { new() { PaymentId = 1 }, new() { PaymentId = 2 } }, 42));

        var result = await _controller.GetList(null, null);

        var page = Assert.IsType<PagedResponse<PaymentDto>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal([1, 2], page.Items.Select(p => p.PaymentId));
        Assert.Equal(42, page.TotalCount);
    }

    [Fact]
    public async Task GetBySource_Payments_MapsToDtos()
    {
        _paymentService.GetBySource(5).Returns(new List<Payment> { new() { PaymentId = 1, PaymentCategory = new Category(7) } });

        var result = await _controller.GetBySource(5);

        var dto = Assert.Single(Assert.IsType<List<PaymentDto>>(Assert.IsType<OkObjectResult>(result).Value));
        Assert.Equal(1, dto.PaymentId);
        Assert.Equal(PaymentDto.CategorySourcePayment, dto.CategorySource);
    }

    [Fact]
    public async Task UploadFile_File_PassesNameStreamAndCancellation()
    {
        var file = Substitute.For<IFormFile>();
        var stream = new MemoryStream();
        file.FileName.Returns("выписка.xlsx");
        file.OpenReadStream().Returns(stream);
        using var cts = new CancellationTokenSource();

        var result = await _controller.UploadFile(file, cts.Token);

        Assert.IsType<OkResult>(result);
        await _paymentsImportService.Received(1).Upload("выписка.xlsx", stream, cts.Token);
    }

    private static DateTime? ParseDate(string? value)
        => value is null ? null : DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
