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
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITransaction _transaction = Substitute.For<ITransaction>();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));

        _categoryRepository.GetById(7).Returns(new Category(7) { CategoryName = "Еда" });
        _accountRepository.GetById(5).Returns(new Account(5, "Карта", "Карта", Now, null));
        _tagRepository.GetAll().Returns(new List<Tag> { new(1, "Поездка", Now, null), new(2, "Ремонт", Now, null) });

        _unitOfWork.BeginTransaction(Arg.Any<CancellationToken>()).Returns(_transaction);

        _service = new PaymentService(_paymentRepository, _categoryRepository, _accountRepository, _tagRepository, _unitOfWork, timeProvider);
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
            _service.GetListPaged(October.From, October.To, null, false, "date", "desc", page, pageSize));
    }

    [Fact]
    public async Task Edit_AllFieldsNull_KeepsFieldsAndTags()
    {
        GivenPayment();

        await _service.Edit(new PaymentEdit(10));

        await _paymentRepository.Received(1).Update(Arg.Is<Payment[]>(p => p.Length == 1
            && p[0].PaymentDescription == "Обед"
            && p[0].ExcludeFromStats
            && p[0].PaymentCategory!.CategoryId == 7
            && p[0].Account!.AccountId == 5
            && p[0].LastUpdatedAt == Now));
        await _paymentRepository.DidNotReceiveWithAnyArgs().SetPaymentTags(default, default!);
        await _transaction.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Edit_NewValues_AppliesThem()
    {
        GivenPayment();
        _categoryRepository.GetById(9).Returns(new Category(9) { CategoryName = "Кафе" });

        await _service.Edit(new PaymentEdit(10) { Description = "Ужин", ExcludeFromStats = false, CategoryId = 9, AccountId = 0 });

        await _paymentRepository.Received(1).Update(Arg.Is<Payment[]>(p => p[0].PaymentDescription == "Ужин"
            && !p[0].ExcludeFromStats
            && p[0].PaymentCategory!.CategoryId == 9
            && p[0].Account == null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Edit_EmptyDescription_ClearsDescription(string description)
    {
        GivenPayment();

        await _service.Edit(new PaymentEdit(10) { Description = description });

        await _paymentRepository.Received(1).Update(Arg.Is<Payment[]>(p => p[0].PaymentDescription == null));
    }

    [Fact]
    public async Task Edit_CategoryOfSource_KeepsOwnCategory()
    {
        GivenPayment();

        await _service.Edit(new PaymentEdit(10) { CategoryId = 8 });

        await _paymentRepository.Received(1).Update(Arg.Is<Payment[]>(p => p[0].PaymentCategory!.CategoryId == 7));
    }

    [Fact]
    public async Task Edit_TagIds_UpdatesAndReplacesTagsInOneTransaction()
    {
        GivenPayment();

        await _service.Edit(new PaymentEdit(10) { TagIds = [1, 2] });

        Received.InOrder(() =>
        {
            _unitOfWork.BeginTransaction(Arg.Any<CancellationToken>());
            _paymentRepository.Update(Arg.Any<Payment[]>());
            _paymentRepository.SetPaymentTags(10, Arg.Is<IReadOnlyCollection<int>>(t => t.SequenceEqual(new[] { 1, 2 })));
            _transaction.Commit(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Edit_EmptyTagIds_ClearsTags()
    {
        GivenPayment();

        await _service.Edit(new PaymentEdit(10) { TagIds = [] });

        await _paymentRepository.Received(1).SetPaymentTags(10, Arg.Is<IReadOnlyCollection<int>>(t => t.Count == 0));
    }

    [Fact]
    public async Task Edit_UnknownTag_ThrowsValidationExceptionWithoutWrite()
    {
        GivenPayment();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.Edit(new PaymentEdit(10) { Description = "Ужин", TagIds = [1, 99] }));

        Assert.Contains("99", ex.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().BeginTransaction(default);
        await _paymentRepository.DidNotReceiveWithAnyArgs().Update();
        await _paymentRepository.DidNotReceiveWithAnyArgs().SetPaymentTags(default, default!);
    }

    [Fact]
    public async Task Edit_UnknownCategoryOrAccount_ThrowsValidationExceptionWithoutWrite()
    {
        GivenPayment();

        await Assert.ThrowsAsync<ValidationException>(() => _service.Edit(new PaymentEdit(10) { CategoryId = 100 }));
        await Assert.ThrowsAsync<ValidationException>(() => _service.Edit(new PaymentEdit(10) { AccountId = 100 }));

        await _paymentRepository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Edit_UnknownPayment_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Edit(new PaymentEdit(99) { Description = "Ужин" }));

        await _paymentRepository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Edit_TagsWriteFails_DoesNotCommit()
    {
        GivenPayment();
        _paymentRepository.SetPaymentTags(10, Arg.Any<IReadOnlyCollection<int>>())
            .Returns(Task.FromException(new InvalidOperationException("БД недоступна")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.Edit(new PaymentEdit(10) { TagIds = [1] }));

        await _transaction.DidNotReceiveWithAnyArgs().Commit(default);
        await _transaction.Received(1).DisposeAsync();
    }

    // Платёж со своей категорией 7, счётом 5 и источником с категорией 8.
    private void GivenPayment()
    {
        // Новый экземпляр на каждый вызов: Edit меняет полученный платёж.
        _paymentRepository.Get(10).Returns(_ => new Payment
        {
            PaymentId = 10,
            PaymentDescription = "Обед",
            ExcludeFromStats = true,
            PaymentCategory = new Category(7),
            Account = new Account { AccountId = 5 },
            PaymentSource = new PaymentSource(3, categoryId: 8)
        });
    }
}
