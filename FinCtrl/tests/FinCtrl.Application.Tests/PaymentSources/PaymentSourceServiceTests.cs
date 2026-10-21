using FinCtrl.Application.Categories;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Domain;
using FinCtrl.Infrastructure.PaymentSources;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Application.Tests.PaymentSources;

public class PaymentSourceServiceTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    private readonly IPaymentSourceRepository _repository = Substitute.For<IPaymentSourceRepository>();
    private readonly ICategoryService _categoryService = Substitute.For<ICategoryService>();
    private readonly PaymentSourceService _service;

    public PaymentSourceServiceTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));

        _service = new PaymentSourceService(_repository, _categoryService, timeProvider);
    }

    [Fact]
    public async Task Edit_WithCategory_UpdatesSourceWithLoadedCategory()
    {
        var food = new Category(7) { CategoryName = "Еда" };
        _categoryService.GetById(7).Returns(food);

        await _service.Edit(42, "Магазин", 7, "Продукты");

        await _repository.Received(1).Update(Arg.Is<PaymentSource[]>(sources =>
            sources.Length == 1
            && sources[0].PaymentSourceId == 42
            && sources[0].PaymentSourceName == "Магазин"
            && sources[0].Description == "Продукты"
            && sources[0].PaymentSourceCategory == food
            && sources[0].LastUpdatedAt == Now));
    }

    [Fact]
    public async Task Edit_WithoutCategory_ClearsCategoryAndDoesNotLoadIt()
    {
        await _service.Edit(42, "Магазин", categoryId: null, description: null);

        await _categoryService.DidNotReceive().GetById(Arg.Any<int>());
        await _repository.Received(1).Update(Arg.Is<PaymentSource[]>(sources =>
            sources.Length == 1
            && sources[0].PaymentSourceId == 42
            && sources[0].PaymentSourceCategory == null
            && sources[0].Description == null
            && sources[0].LastUpdatedAt == Now));
    }
}
