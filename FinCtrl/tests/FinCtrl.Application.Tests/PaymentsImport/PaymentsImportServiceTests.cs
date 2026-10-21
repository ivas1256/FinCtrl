using FinCtrl.Application.Accounts;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport;
using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Common;
using FinCtrl.Domain;
using FinCtrl.Domain.PaymentsImport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinCtrl.Application.Tests.PaymentsImport;

public class PaymentsImportServiceTests
{
    private static readonly DateTime Now = new(2025, 10, 15, 14, 30, 0);
    private static readonly DateTime PaymentDate = new(2025, 10, 1);

    private readonly IPaymentSourceService _paymentSourceService = Substitute.For<IPaymentSourceService>();
    private readonly IAccountService _accountService = Substitute.For<IAccountService>();
    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly IFileFormatFactory _fileFormatFactory = Substitute.For<IFileFormatFactory>();
    private readonly IFileFormat _fileFormat = Substitute.For<IFileFormat>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITransaction _transaction = Substitute.For<ITransaction>();
    private readonly FakeLogger<PaymentsImportService> _logger = new();
    private readonly PaymentsImportService _service;

    private readonly PaymentSource _shop = new(1, "Магазин", null, null, Now, null);
    private readonly Account _card = new(5, "Карта", "Карта", Now, null);

    public PaymentsImportServiceTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));

        _fileFormatFactory.Create(Arg.Any<string>()).Returns(_fileFormat);
        _unitOfWork.BeginTransaction(Arg.Any<CancellationToken>()).Returns(_transaction);

        _paymentSourceService.GetByName("Магазин").Returns(_shop);
        _accountService.GetByExcelName("Карта").Returns(_card);

        _service = new PaymentsImportService(_paymentSourceService, _accountService, _paymentService, _fileFormatFactory,
            _unitOfWork, timeProvider, _logger);
    }

    [Fact]
    public async Task Upload_FileName_SelectsFormatByExtension()
    {
        _fileFormat.GetFileNodes(Arg.Any<Stream>()).Returns([]);

        await _service.Upload("выписка.XLSX", new MemoryStream());

        _fileFormatFactory.Received(1).Create(".XLSX");
    }

    [Fact]
    public async Task Upload_NoDuplicate_CreatesPaymentWithSourceAccountAndType()
    {
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", "INV-1", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.Received(1).Create(Arg.Is<Payment>(p =>
            p.PaymentSum == -150m
            && p.PaymentDate == PaymentDate
            && p.PaymentType == PaymentTypeEnum.Spending
            && p.PaymentSource == _shop
            && p.Account == _card
            && p.CreatedAt == Now));
        await _paymentService.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Upload_NewSourceAndAccount_CreatesThemBeforePayment()
    {
        var newSource = new PaymentSource(2, "Кафе", null, null, Now, null);
        var newAccount = new Account(6, "Счёт", "Счёт", Now, null);
        _paymentSourceService.Create(Arg.Any<PaymentSource>()).Returns(2);
        _paymentSourceService.GetById(2).Returns(newSource);
        _accountService.Create(Arg.Any<Account>()).Returns(6);
        _accountService.GetById(6).Returns(newAccount);
        SetRows(new ExcelPayment(PaymentDate, 300m, "Кафе", "INV-2", "OK", "Пополнение", "Счёт"));

        await _service.Upload("file.csv", new MemoryStream());

        await _paymentSourceService.Received(1).Create(Arg.Is<PaymentSource>(s => s.PaymentSourceName == "Кафе" && s.CreatedAt == Now));
        await _accountService.Received(1).Create(Arg.Is<Account>(a => a.ExcelAccountName == "Счёт" && a.CreatedAt == Now));
        await _paymentService.Received(1).Create(Arg.Is<Payment>(p =>
            p.PaymentSource == newSource && p.Account == newAccount && p.PaymentType == PaymentTypeEnum.Income));
    }

    [Fact]
    public async Task Upload_SameSourceAndAccountInSeveralRows_LoadsThemOnce()
    {
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", "INV-1", "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate.AddDays(1), -200m, "Магазин", "INV-2", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentSourceService.Received(1).GetByName("Магазин");
        await _accountService.Received(1).GetByExcelName("Карта");
        await _paymentService.Received(2).Create(Arg.Any<Payment>());
    }

    [Fact]
    public async Task Upload_DuplicateWithoutAccount_FillsAccountInsteadOfCreating()
    {
        var existing = new Payment { PaymentId = 10, PaymentSum = -150m, PaymentDate = PaymentDate, Account = null };
        _paymentService.FindDuplicate(-150m, PaymentDate).Returns(existing);
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", "INV-1", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.DidNotReceive().Create(Arg.Any<Payment>());
        await _paymentService.Received(1).Update(Arg.Is<Payment[]>(ps => ps.Length == 1 && ps[0] == existing && ps[0].Account == _card));
    }

    [Fact]
    public async Task Upload_DuplicateWithAccount_LeavesPaymentUntouched()
    {
        var otherAccount = new Account(7, "Другая", "Другая", Now, null);
        var existing = new Payment { PaymentId = 10, PaymentSum = -150m, PaymentDate = PaymentDate, Account = otherAccount };
        _paymentService.FindDuplicate(-150m, PaymentDate).Returns(existing);
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", "INV-1", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.DidNotReceive().Create(Arg.Any<Payment>());
        await _paymentService.DidNotReceiveWithAnyArgs().Update();
        Assert.Same(otherAccount, existing.Account);
    }

    [Fact]
    public async Task Upload_AllRowsImported_CommitsSingleTransaction()
    {
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", "INV-1", "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate.AddDays(1), -200m, "Магазин", "INV-2", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _unitOfWork.Received(1).BeginTransaction(Arg.Any<CancellationToken>());
        await _transaction.Received(1).Commit(Arg.Any<CancellationToken>());
        await _transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Upload_ErrorOnSecondRow_DoesNotCommitAndDisposesTransaction()
    {
        var failingDate = PaymentDate.AddDays(1);
        _paymentService.FindDuplicate(-200m, failingDate).ThrowsAsync(new InvalidOperationException("ошибка БД"));
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", "INV-1", "OK", "Списание", "Карта"),
            new ExcelPayment(failingDate, -200m, "Магазин", "INV-2", "OK", "Списание", "Карта"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.Upload("file.xlsx", new MemoryStream()));

        await _paymentService.Received(1).Create(Arg.Any<Payment>());
        await _transaction.DidNotReceiveWithAnyArgs().Commit();
        await _transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Upload_Cancelled_DoesNotImportOrCommit()
    {
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", "INV-1", "OK", "Списание", "Карта"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.Upload("file.xlsx", new MemoryStream(), cts.Token));

        await _paymentService.DidNotReceive().Create(Arg.Any<Payment>());
        await _transaction.DidNotReceiveWithAnyArgs().Commit();
    }

    [Fact]
    public async Task Upload_Imported_LogsSummary()
    {
        var existingWithoutAccount = new Payment { PaymentId = 10, PaymentSum = -200m, PaymentDate = PaymentDate, Account = null };
        var existingWithAccount = new Payment { PaymentId = 11, PaymentSum = -300m, PaymentDate = PaymentDate, Account = _card };
        _paymentService.FindDuplicate(-200m, PaymentDate).Returns(existingWithoutAccount);
        _paymentService.FindDuplicate(-300m, PaymentDate).Returns(existingWithAccount);
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", "INV-1", "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate, -200m, "Магазин", "INV-2", "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate, -300m, "Магазин", "INV-3", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal(
            "Импорт файла file.xlsx: строк 3, создано платежей 1, дозаполнен счёт 1, пропущено дублей 1",
            record.Message);
    }

    private void SetRows(params ExcelPayment[] rows) =>
        _fileFormat.GetFileNodes(Arg.Any<Stream>()).Returns(rows.ToList());
}
