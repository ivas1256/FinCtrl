using FinCtrl.Application.Categories;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Common.Application;
using FinCtrl.Domain;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Application.Tests.PaymentSources;

public class PaymentSourceServiceTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);

    private readonly IPaymentSourceRepository _repository = Substitute.For<IPaymentSourceRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly PaymentSourceService _service;

    public PaymentSourceServiceTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));

        _repository.GetById(42).Returns(new PaymentSource(42, "Старое имя", null, null, Now, null));
        _categoryRepository.GetById(7).Returns(new Category(7) { CategoryName = "Еда" });

        _service = new PaymentSourceService(_repository, _categoryRepository, timeProvider);
    }

    [Fact]
    public async Task GetAll_ReturnsCategoriesLoadedByRepositoryWithoutExtraQueries()
    {
        var food = new Category(7) { CategoryName = "Еда", Color = "#00ff00" };
        var shop = new PaymentSource(1, "Магазин", null, null, Now, null) { PaymentSourceCategory = food };
        var cafe = new PaymentSource(2, "Кафе", null, null, Now, null);
        _repository.GetAll().Returns(new List<PaymentSource> { shop, cafe });

        var result = await _service.GetAll();

        Assert.Equal(new[] { 2, 1 }, result.Select(x => x.PaymentSourceId));
        Assert.Null(result[0].PaymentSourceCategory);
        Assert.Same(food, result[1].PaymentSourceCategory);
    }

    [Fact]
    public async Task GetById_ReturnsCategoryLoadedByRepositoryWithoutExtraQueries()
    {
        var food = new Category(7) { CategoryName = "Еда" };
        var shop = new PaymentSource(1, "Магазин", null, null, Now, null) { PaymentSourceCategory = food };
        _repository.GetById(1).Returns(shop);

        var result = await _service.GetById(1);

        Assert.NotNull(result);
        Assert.Same(food, result.PaymentSourceCategory);
        Assert.Equal("Еда", result.PaymentSourceCategory!.CategoryName);
    }

    [Fact]
    public async Task GetByName_ReturnsCategoryLoadedByRepositoryWithoutExtraQueries()
    {
        var food = new Category(7) { CategoryName = "Еда" };
        var shop = new PaymentSource(1, "Магазин", null, null, Now, null) { PaymentSourceCategory = food };
        _repository.GetByName("Магазин").Returns(shop);

        var result = await _service.GetByName("Магазин");

        Assert.NotNull(result);
        Assert.Same(food, result.PaymentSourceCategory);
    }

    [Fact]
    public async Task GetById_UnknownSource_ThrowsNotFoundException()
    {
        _repository.GetById(1).Returns((PaymentSource?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetById(1));
    }

    [Fact]
    public async Task Edit_UnknownSource_ThrowsNotFoundExceptionWithoutUpdate()
    {
        _repository.GetById(99).Returns((PaymentSource?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.Edit(99, "Магазин", null, null));

        await _repository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Edit_UnknownCategory_ThrowsValidationExceptionWithoutUpdate()
    {
        _categoryRepository.GetById(100).Returns((Category?)null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.Edit(42, "Магазин", 100, null));

        Assert.Contains("100", ex.Message);
        await _repository.DidNotReceiveWithAnyArgs().Update();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Edit_EmptyName_ThrowsValidationException(string name)
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.Edit(42, name, null, null));

        await _repository.DidNotReceiveWithAnyArgs().Update();
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    public async Task GetListPaged_InvalidPaging_ThrowsValidationException(int page, int? pageSize)
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.GetListPaged(null, false, page, pageSize));
    }

    [Fact]
    public async Task GetListPaged_PageSizeNull_ReturnsAllFromRepository()
    {
        _repository.GetListPaged(null, false, 1, null).Returns((new List<PaymentSource>(), 0));

        var (items, total) = await _service.GetListPaged(null, false, 1, null);

        Assert.Empty(items);
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task Edit_WithCategory_UpdatesSourceWithCategoryId()
    {
        await _service.Edit(42, "Магазин", 7, "Продукты");

        await _repository.Received(1).Update(Arg.Is<PaymentSource[]>(sources =>
            sources.Length == 1
            && sources[0].PaymentSourceId == 42
            && sources[0].PaymentSourceName == "Магазин"
            && sources[0].Description == "Продукты"
            && sources[0].PaymentSourceCategory!.CategoryId == 7
            && sources[0].LastUpdatedAt == Now));
    }

    [Fact]
    public async Task Edit_WithoutCategory_ClearsCategory()
    {
        await _service.Edit(42, "Магазин", categoryId: null, description: null);

        await _repository.Received(1).Update(Arg.Is<PaymentSource[]>(sources =>
            sources.Length == 1
            && sources[0].PaymentSourceId == 42
            && sources[0].PaymentSourceCategory == null
            && sources[0].Description == null
            && sources[0].LastUpdatedAt == Now));
    }
}
