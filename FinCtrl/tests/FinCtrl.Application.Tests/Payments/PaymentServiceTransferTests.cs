using FinCtrl.Application.Accounts;
using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Application.Tags;
using FinCtrl.Application.Transfers;
using FinCtrl.Common;
using FinCtrl.Common.Application;
using FinCtrl.Domain;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Application.Tests.Payments;

/// <summary>Переводы между своими счетами: отметка, связь сторон и подбор кандидатов в <see cref="PaymentService"/>.</summary>
public class PaymentServiceTransferTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);
    private static readonly DateTime Day = new(2025, 10, 10, 12, 0, 0);

    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly ITransferRepository _transferRepository = Substitute.For<ITransferRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITransaction _transaction = Substitute.For<ITransaction>();
    private readonly PaymentService _service;

    // Списание −500 (id 1) и поступление +500 (id 2)
    private readonly Payment _outgoing = new() { PaymentId = 1, PaymentType = PaymentTypeEnum.Spending, PaymentSum = -500m, PaymentDate = Day };
    private readonly Payment _incoming = new() { PaymentId = 2, PaymentType = PaymentTypeEnum.Income, PaymentSum = 500m, PaymentDate = Day };

    public PaymentServiceTransferTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));

        _unitOfWork.BeginTransaction(Arg.Any<CancellationToken>()).Returns(_transaction);
        _paymentRepository.Get(1).Returns(_outgoing);
        _paymentRepository.Get(2).Returns(_incoming);

        _service = new PaymentService(_paymentRepository, Substitute.For<ICategoryRepository>(), Substitute.For<IAccountRepository>(),
            Substitute.For<ITagRepository>(), _transferRepository, _unitOfWork, timeProvider);
    }

    private static bool IsRow(Transfer t, int outId, int? inId) => t.OutPaymentId == outId && t.InPaymentId == inId && t.CreatedAt == Now;

    // --- Edit: отметка без второй стороны ---

    [Fact]
    public async Task Edit_MarkOutgoingWithoutPair_TransferTypeAndRowWithoutIncoming()
    {
        await _service.Edit(new PaymentEdit(1) { IsTransfer = true });

        Assert.Equal(PaymentTypeEnum.Transfer, _outgoing.PaymentType);
        await _paymentRepository.Received(1).Update(Arg.Is<Payment[]>(ps => ps.Single() == _outgoing));
        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 1, null)));
        await _transaction.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Edit_MarkIncomingWithoutPair_TransferTypeWithoutRow()
    {
        await _service.Edit(new PaymentEdit(2) { IsTransfer = true });

        Assert.Equal(PaymentTypeEnum.Transfer, _incoming.PaymentType);
        await _transferRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Fact]
    public async Task Edit_AlreadyTransferMarkedAgain_RowsUntouched()
    {
        _outgoing.PaymentType = PaymentTypeEnum.Transfer;

        await _service.Edit(new PaymentEdit(1) { IsTransfer = true });

        await _transferRepository.DidNotReceiveWithAnyArgs().Create(default!);
        await _transferRepository.DidNotReceiveWithAnyArgs().Delete(default);
    }

    [Fact]
    public async Task Edit_TransferFieldsNull_TypeAndRowsUntouched()
    {
        _outgoing.PaymentType = PaymentTypeEnum.Transfer;

        await _service.Edit(new PaymentEdit(1) { Description = "Перевод" });

        Assert.Equal(PaymentTypeEnum.Transfer, _outgoing.PaymentType);
        await _transferRepository.DidNotReceiveWithAnyArgs().GetByPaymentId(default);
        await _transferRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    // --- Edit: связь сторон ---

    [Fact]
    public async Task Edit_OutgoingWithPair_BothTransferAndLinkedRow()
    {
        await _service.Edit(new PaymentEdit(1) { IsTransfer = true, TransferPairPaymentId = 2 });

        Assert.Equal(PaymentTypeEnum.Transfer, _outgoing.PaymentType);
        Assert.Equal(PaymentTypeEnum.Transfer, _incoming.PaymentType);
        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 1, 2)));
        await _paymentRepository.Received(1).Update(Arg.Is<Payment[]>(ps => ps.Single() == _incoming));
        Assert.Equal(Now, _incoming.LastUpdatedAt);
        await _transaction.Received(1).Commit(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Edit_IncomingWithPair_OutgoingSideIsOut()
    {
        await _service.Edit(new PaymentEdit(2) { IsTransfer = true, TransferPairPaymentId = 1 });

        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 1, 2)));
        Assert.Equal(PaymentTypeEnum.Transfer, _outgoing.PaymentType);
    }

    [Fact]
    public async Task Edit_PairWithoutIsTransferOnTransfer_Links()
    {
        _outgoing.PaymentType = PaymentTypeEnum.Transfer;

        await _service.Edit(new PaymentEdit(1) { TransferPairPaymentId = 2 });

        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 1, 2)));
    }

    [Fact]
    public async Task Edit_LinkBothSidesWithoutPair_OldRowsReplacedByLinkedRow()
    {
        _outgoing.PaymentType = PaymentTypeEnum.Transfer;
        _transferRepository.GetByPaymentId(1).Returns(new Transfer { TransferId = 7, OutPaymentId = 1, InPaymentId = null });

        await _service.Edit(new PaymentEdit(2) { IsTransfer = true, TransferPairPaymentId = 1 });

        await _transferRepository.Received(1).Delete(7);
        await _transferRepository.Received(1).Create(Arg.Any<Transfer>());
        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 1, 2)));
    }

    [Fact]
    public async Task Edit_AlreadyLinkedTogether_RowsUntouched()
    {
        var row = new Transfer { TransferId = 7, OutPaymentId = 1, InPaymentId = 2 };
        _outgoing.PaymentType = _incoming.PaymentType = PaymentTypeEnum.Transfer;
        _transferRepository.GetByPaymentId(1).Returns(row);
        _transferRepository.GetByPaymentId(2).Returns(row);

        await _service.Edit(new PaymentEdit(1) { IsTransfer = true, TransferPairPaymentId = 2 });

        await _transferRepository.DidNotReceiveWithAnyArgs().Delete(default);
        await _transferRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Fact]
    public async Task Edit_RelinkIncomingToOtherOutgoing_OldOutgoingKeepsRowWithoutPair()
    {
        // Поступление 2 было связано со списанием 3; связываем его со списанием 1
        var oldOutgoing = new Payment { PaymentId = 3, PaymentType = PaymentTypeEnum.Transfer, PaymentSum = -500m, PaymentDate = Day };
        _paymentRepository.Get(3).Returns(oldOutgoing);
        _incoming.PaymentType = PaymentTypeEnum.Transfer;
        _transferRepository.GetByPaymentId(2).Returns(new Transfer { TransferId = 7, OutPaymentId = 3, InPaymentId = 2 });

        await _service.Edit(new PaymentEdit(2) { TransferPairPaymentId = 1 });

        await _transferRepository.Received(1).Delete(7);
        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 3, null)));
        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 1, 2)));
    }

    // --- Edit: отвязка и снятие отметки ---

    [Fact]
    public async Task Edit_UnlinkFromOutgoing_OutgoingStaysTransferWithoutPair()
    {
        _outgoing.PaymentType = _incoming.PaymentType = PaymentTypeEnum.Transfer;
        _transferRepository.GetByPaymentId(1).Returns(new Transfer { TransferId = 7, OutPaymentId = 1, InPaymentId = 2 });

        await _service.Edit(new PaymentEdit(1) { TransferPairPaymentId = 0 });

        Assert.Equal(PaymentTypeEnum.Transfer, _outgoing.PaymentType);
        await _transferRepository.Received(1).Delete(7);
        await _transferRepository.Received(1).Create(Arg.Any<Transfer>());
        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 1, null)));
        await _paymentRepository.DidNotReceive().Update(Arg.Is<Payment[]>(ps => ps.Contains(_incoming)));
    }

    [Fact]
    public async Task Edit_UnlinkFromIncoming_OutgoingPartnerKeepsRowWithoutPair()
    {
        _outgoing.PaymentType = _incoming.PaymentType = PaymentTypeEnum.Transfer;
        _transferRepository.GetByPaymentId(2).Returns(new Transfer { TransferId = 7, OutPaymentId = 1, InPaymentId = 2 });

        await _service.Edit(new PaymentEdit(2) { TransferPairPaymentId = 0 });

        Assert.Equal(PaymentTypeEnum.Transfer, _incoming.PaymentType);
        await _transferRepository.Received(1).Delete(7);
        await _transferRepository.Received(1).Create(Arg.Any<Transfer>());
        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 1, null)));
    }

    [Theory]
    [InlineData(1, PaymentTypeEnum.Spending)]
    [InlineData(2, PaymentTypeEnum.Income)]
    public async Task Edit_UncheckTransfer_TypeBySignAndLinkRemoved(int paymentId, PaymentTypeEnum expectedType)
    {
        _outgoing.PaymentType = _incoming.PaymentType = PaymentTypeEnum.Transfer;
        _transferRepository.GetByPaymentId(paymentId).Returns(new Transfer { TransferId = 7, OutPaymentId = 1, InPaymentId = 2 });
        var payment = paymentId == 1 ? _outgoing : _incoming;

        await _service.Edit(new PaymentEdit(paymentId) { IsTransfer = false });

        Assert.Equal(expectedType, payment.PaymentType);
        await _transferRepository.Received(1).Delete(7);
        await _transferRepository.DidNotReceive().Create(Arg.Is<Transfer>(t => t.OutPaymentId == paymentId));
    }

    [Fact]
    public async Task Edit_UncheckOnIncoming_OutgoingPartnerStaysTransferWithRowWithoutPair()
    {
        _outgoing.PaymentType = _incoming.PaymentType = PaymentTypeEnum.Transfer;
        _transferRepository.GetByPaymentId(2).Returns(new Transfer { TransferId = 7, OutPaymentId = 1, InPaymentId = 2 });

        await _service.Edit(new PaymentEdit(2) { IsTransfer = false });

        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 1, null)));
        await _paymentRepository.DidNotReceive().Update(Arg.Is<Payment[]>(ps => ps.Contains(_outgoing)));
    }

    [Fact]
    public async Task Edit_UncheckNotTransfer_NothingChanges()
    {
        await _service.Edit(new PaymentEdit(1) { IsTransfer = false });

        Assert.Equal(PaymentTypeEnum.Spending, _outgoing.PaymentType);
        await _transferRepository.DidNotReceiveWithAnyArgs().Delete(default);
    }

    // --- Edit: валидация ---

    [Fact]
    public async Task Edit_PairForNotTransfer_ValidationAndNothingWritten()
    {
        await AssertRejected<ValidationException>(new PaymentEdit(1) { TransferPairPaymentId = 2 });
    }

    [Fact]
    public async Task Edit_PairWithUncheck_ValidationAndNothingWritten()
    {
        await AssertRejected<ValidationException>(new PaymentEdit(1) { IsTransfer = false, TransferPairPaymentId = 2 });
    }

    [Fact]
    public async Task Edit_PairIsSelf_ValidationAndNothingWritten()
    {
        await AssertRejected<ValidationException>(new PaymentEdit(1) { IsTransfer = true, TransferPairPaymentId = 1 });
    }

    [Fact]
    public async Task Edit_PairNotFound_ValidationAndNothingWritten()
    {
        await AssertRejected<ValidationException>(new PaymentEdit(1) { IsTransfer = true, TransferPairPaymentId = 99 });
    }

    [Fact]
    public async Task Edit_PairSameSign_ValidationAndNothingWritten()
    {
        _paymentRepository.Get(3).Returns(new Payment { PaymentId = 3, PaymentType = PaymentTypeEnum.Spending, PaymentSum = -500m });

        await AssertRejected<ValidationException>(new PaymentEdit(1) { IsTransfer = true, TransferPairPaymentId = 3 });
    }

    [Fact]
    public async Task Edit_ZeroSum_ValidationAndNothingWritten()
    {
        _outgoing.PaymentSum = 0m;

        await AssertRejected<ValidationException>(new PaymentEdit(1) { IsTransfer = true, TransferPairPaymentId = 2 });
    }

    [Fact]
    public async Task Edit_PairLinkedWithOtherPayment_ConflictAndNothingWritten()
    {
        _transferRepository.GetByPaymentId(2).Returns(new Transfer { TransferId = 7, OutPaymentId = 3, InPaymentId = 2 });

        await AssertRejected<ConflictException>(new PaymentEdit(1) { IsTransfer = true, TransferPairPaymentId = 2 });
    }

    [Fact]
    public async Task Edit_PairIsOutgoingWithoutPair_LinksWithoutConflict()
    {
        // Списание-перевод без второй стороны — не «в другом переводе», его можно связать
        _outgoing.PaymentType = PaymentTypeEnum.Transfer;
        _transferRepository.GetByPaymentId(1).Returns(new Transfer { TransferId = 7, OutPaymentId = 1, InPaymentId = null });

        await _service.Edit(new PaymentEdit(2) { IsTransfer = true, TransferPairPaymentId = 1 });

        await _transferRepository.Received(1).Create(Arg.Is<Transfer>(t => IsRow(t, 1, 2)));
    }

    private async Task AssertRejected<TException>(PaymentEdit edit) where TException : Exception
    {
        await Assert.ThrowsAsync<TException>(() => _service.Edit(edit));

        await _unitOfWork.DidNotReceiveWithAnyArgs().BeginTransaction();
        await _paymentRepository.DidNotReceiveWithAnyArgs().Update();
        await _transferRepository.DidNotReceiveWithAnyArgs().Create(default!);
        await _transferRepository.DidNotReceiveWithAnyArgs().Delete(default);
    }

    // --- Подбор кандидатов ---

    [Fact]
    public async Task GetTransferCandidates_Window_PassesAbsSumDaysAndAccount()
    {
        _outgoing.Account = new Account { AccountId = 5 };
        _paymentRepository.GetTransferCandidates(default, default, default, default, default).ReturnsForAnyArgs(new List<Payment>());

        await _service.GetTransferCandidates(1);

        await _paymentRepository.Received(1).GetTransferCandidates(1, 500m, new DateTime(2025, 10, 5), new DateTime(2025, 10, 16), 5);
    }

    [Fact]
    public async Task GetTransferCandidates_OppositeSignOnly_SortedByDateProximityThenId()
    {
        // Расход, сохранённый с положительной суммой, после нормализации отрицательный — того же знака, что и платёж 1
        var normalizedSpending = new Payment { PaymentId = 10, PaymentType = PaymentTypeEnum.Spending, PaymentSum = 500m, PaymentDate = Day };
        normalizedSpending.NormalizeSumSign();
        var far = new Payment { PaymentId = 11, PaymentSum = 500m, PaymentDate = Day.AddDays(-4) };
        var nearLaterId = new Payment { PaymentId = 13, PaymentSum = 500m, PaymentDate = Day.AddDays(1) };
        var nearEarlierId = new Payment { PaymentId = 12, PaymentSum = 500m, PaymentDate = Day.AddDays(-1) };
        _paymentRepository.GetTransferCandidates(default, default, default, default, default)
            .ReturnsForAnyArgs(new List<Payment> { normalizedSpending, far, nearLaterId, nearEarlierId });

        var candidates = await _service.GetTransferCandidates(1);

        Assert.Equal(new[] { 12, 13, 11 }, candidates.Select(c => c.PaymentId));
    }

    [Fact]
    public async Task GetTransferCandidates_ZeroSum_EmptyWithoutQuery()
    {
        _outgoing.PaymentSum = 0m;

        Assert.Empty(await _service.GetTransferCandidates(1));
        await _paymentRepository.DidNotReceiveWithAnyArgs().GetTransferCandidates(default, default, default, default, default);
    }

    [Fact]
    public async Task GetTransferCandidates_PaymentNotFound_Throws()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetTransferCandidates(99));
    }
}
