using FinCtrl.Application.Accounts;
using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Application.Tags;
using FinCtrl.Common;
using FinCtrl.Common.Application;
using FinCtrl.Domain;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Application.Tests.Payments;

public class PaymentServiceTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);
    private static readonly DateRange October = new(new DateTime(2025, 10, 1), new DateTime(2025, 11, 1));

    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));

        _categoryRepository.GetById(7).Returns(new Category(7) { CategoryName = "Еда" });
        _accountRepository.GetById(5).Returns(new Account(5, "Карта", "Карта", Now, null));
        _tagRepository.GetAll().Returns(new List<Tag> { new(1, "Поездка", Now, null), new(2, "Ремонт", Now, null) });

        _service = new PaymentService(_paymentRepository, _categoryRepository, _accountRepository, _tagRepository, timeProvider);
    }

    [Fact]
    public async Task Get_UnknownPayment_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Get(99));
    }

    [Fact]
    public async Task Update_ExistingReferences_SavesWithUpdatedAt()
    {
        var payment = new Payment { PaymentId = 10, PaymentCategory = new Category(7), Account = new Account { AccountId = 5 } };

        await _service.Update(payment);

        await _paymentRepository.Received(1).Update(Arg.Is<Payment[]>(p => p.Length == 1 && p[0].LastUpdatedAt == Now));
    }

    [Fact]
    public async Task Update_UnknownCategory_ThrowsValidationExceptionWithoutUpdate()
    {
        var payment = new Payment { PaymentId = 10, PaymentCategory = new Category(100) };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.Update(payment));

        Assert.Contains("100", ex.Message);
        await _paymentRepository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Update_UnknownAccount_ThrowsValidationExceptionWithoutUpdate()
    {
        var payment = new Payment { PaymentId = 10, Account = new Account { AccountId = 100 } };

        await Assert.ThrowsAsync<ValidationException>(() => _service.Update(payment));

        await _paymentRepository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task SetPaymentTags_ExistingTags_ReplacesTags()
    {
        await _service.SetPaymentTags(10, [1, 2]);

        await _paymentRepository.Received(1).SetPaymentTags(10, Arg.Is<IReadOnlyCollection<int>>(t => t.SequenceEqual(new[] { 1, 2 })));
    }

    [Fact]
    public async Task SetPaymentTags_EmptyList_ClearsTagsWithoutLookup()
    {
        await _service.SetPaymentTags(10, []);

        await _tagRepository.DidNotReceive().GetAll();
        await _paymentRepository.Received(1).SetPaymentTags(10, Arg.Is<IReadOnlyCollection<int>>(t => t.Count == 0));
    }

    [Fact]
    public async Task SetPaymentTags_UnknownTag_ThrowsValidationExceptionWithoutWrite()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.SetPaymentTags(10, [1, 99]));

        Assert.Contains("99", ex.Message);
        await _paymentRepository.DidNotReceiveWithAnyArgs().SetPaymentTags(default, default!);
    }

    [Fact]
    public async Task BulkAssignTags_MatchingCategory_AddsTagsToMatchingPayments()
    {
        _paymentRepository.GetByDateRange(October).Returns(new List<Payment>
        {
            new() { PaymentId = 1, PaymentCategory = new Category(7) },
            new() { PaymentId = 2, PaymentCategory = new Category(8) }
        });

        var count = await _service.BulkAssignTags(October, [7], [1]);

        Assert.Equal(1, count);
        await _paymentRepository.Received(1).AddTagsToPayments(
            Arg.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 1 })), Arg.Any<IEnumerable<int>>());
    }

    [Fact]
    public async Task BulkAssignTags_UnknownTag_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.BulkAssignTags(October, [7], [99]));

        await _paymentRepository.DidNotReceiveWithAnyArgs().AddTagsToPayments(default!, default!);
    }

    [Fact]
    public async Task BulkAssignTags_NoCategoriesOrTags_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.BulkAssignTags(October, [], [1]));
        await Assert.ThrowsAsync<ValidationException>(() => _service.BulkAssignTags(October, [7], []));
    }

    [Fact]
    public async Task BulkAssignTags_ReversedPeriod_ThrowsValidationException()
    {
        var reversed = new DateRange(October.To, October.From);

        await Assert.ThrowsAsync<ValidationException>(() => _service.BulkAssignTags(reversed, [7], [1]));
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    public async Task GetListPaged_InvalidPaging_ThrowsValidationException(int page, int? pageSize)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GetListPaged(October, null, false, "date", "desc", page, pageSize));
    }
}
