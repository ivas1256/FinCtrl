using FinCtrl.Application.Accounts;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport;
using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Application.PaymentSources.Classification;
using FinCtrl.Common;
using FinCtrl.Common.Application;
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
    private readonly IPaymentSourceClassificationService _classificationService = Substitute.For<IPaymentSourceClassificationService>();
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
        _classificationService.Classify(Arg.Any<IReadOnlyList<PaymentSource>>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        _service = new PaymentsImportService(_paymentSourceService, _accountService, _paymentService, _fileFormatFactory,
            _unitOfWork, _classificationService, timeProvider, _logger);
    }

    [Fact]
    public async Task Upload_NewAndExistingSources_ReturnsOnlyNewSourcesAndCreatedPayments()
    {
        var cafe = new PaymentSource(2, "Кафе", null, null, Now, null);
        _paymentSourceService.Create(Arg.Any<PaymentSource>()).Returns(2);
        _paymentSourceService.GetById(2).Returns(cafe);
        _paymentService.Create(Arg.Any<Payment>()).Returns(100, 101, 102);
        _paymentService.FindDuplicate(-300m, PaymentDate, 5, PaymentTypeEnum.Spending).Returns(new Payment { PaymentId = 7, Account = _card });
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate, -200m, "Кафе", null, "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate.AddDays(1), -50m, "Кафе", null, "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate, -300m, "Магазин", null, "OK", "Списание", "Карта"));

        var result = await _service.Upload("file.xlsx", new MemoryStream());

        Assert.Same(cafe, Assert.Single(result.NewPaymentSources));
        Assert.Equal(new[] { 100, 101, 102 }, result.NewPayments.Select(p => p.PaymentId));
        Assert.Equal(new[] { -150m, -200m, -50m }, result.NewPayments.Select(p => p.PaymentSum));
        Assert.Null(result.ClassificationError);
    }

    [Fact]
    public async Task Upload_NewSources_ClassifiesThemAfterCommitAndReturnsError()
    {
        var cafe = new PaymentSource(2, "Кафе", null, null, Now, null);
        _paymentSourceService.Create(Arg.Any<PaymentSource>()).Returns(2);
        _paymentSourceService.GetById(2).Returns(cafe);
        _classificationService.Classify(Arg.Any<IReadOnlyList<PaymentSource>>(), Arg.Any<CancellationToken>())
            .Returns("сервис недоступен");
        SetRows(new ExcelPayment(PaymentDate, -200m, "Кафе", null, "OK", "Списание", "Карта"));

        var result = await _service.Upload("file.xlsx", new MemoryStream());

        Received.InOrder(() =>
        {
            _transaction.Commit(Arg.Any<CancellationToken>());
            _classificationService.Classify(Arg.Is<IReadOnlyList<PaymentSource>>(s => s.Single() == cafe), Arg.Any<CancellationToken>());
        });
        Assert.Equal("сервис недоступен", result.ClassificationError);
        Assert.Single(result.NewPayments);
    }

    [Fact]
    public async Task Upload_FileName_SelectsFormatByExtension()
    {
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"));

        await _service.Upload("выписка.XLSX", new MemoryStream());

        _fileFormatFactory.Received(1).Create(".XLSX");
    }

    [Fact]
    public async Task Upload_FileWithoutRows_ThrowsValidationExceptionWithoutTransaction()
    {
        SetRows();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.Upload("пусто.csv", new MemoryStream()));

        Assert.Contains("пусто.csv", ex.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().BeginTransaction();
    }

    [Fact]
    public async Task Upload_UnsupportedFormat_PropagatesValidationException()
    {
        _fileFormatFactory.Create(".pdf").Throws(new ValidationException("Неподдерживаемый формат файла"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.Upload("выписка.pdf", new MemoryStream()));

        await _unitOfWork.DidNotReceiveWithAnyArgs().BeginTransaction();
    }

    [Fact]
    public async Task Upload_NoDuplicate_CreatesPaymentWithSourceAccountAndType()
    {
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"));

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
        SetRows(new ExcelPayment(PaymentDate, 300m, "Кафе", null, "OK", "Пополнение", "Счёт"));

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
            new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate.AddDays(1), -200m, "Магазин", null, "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentSourceService.Received(1).GetByName("Магазин");
        await _accountService.Received(1).GetByExcelName("Карта");
        await _paymentService.Received(2).Create(Arg.Any<Payment>());
    }

    [Fact]
    public async Task Upload_DuplicateWithoutAccount_FillsAccountInsteadOfCreating()
    {
        var existing = new Payment { PaymentId = 10, PaymentSum = -150m, PaymentDate = PaymentDate, Account = null };
        _paymentService.FindDuplicate(-150m, PaymentDate, 5, PaymentTypeEnum.Spending).Returns(existing);
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.DidNotReceive().Create(Arg.Any<Payment>());
        await _paymentService.Received(1).Update(Arg.Is<Payment[]>(ps => ps.Length == 1 && ps[0] == existing && ps[0].Account == _card));
    }

    [Fact]
    public async Task Upload_DuplicateOnSameAccount_LeavesPaymentUntouched()
    {
        var existing = new Payment { PaymentId = 10, PaymentSum = -150m, PaymentDate = PaymentDate, Account = _card };
        _paymentService.FindDuplicate(-150m, PaymentDate, 5, PaymentTypeEnum.Spending).Returns(existing);
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.DidNotReceive().Create(Arg.Any<Payment>());
        await _paymentService.DidNotReceiveWithAnyArgs().Update();
        Assert.Same(_card, existing.Account);
    }

    [Fact]
    public async Task Upload_NoDuplicateOnRowAccount_CreatesPayment()
    {
        // Платёж с той же датой и суммой на другом счёте FindDuplicate не возвращает — строка импортируется
        _paymentService.FindDuplicate(-150m, PaymentDate, 5, PaymentTypeEnum.Spending).Returns((Payment?)null);
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.Received(1).FindDuplicate(-150m, PaymentDate, 5, PaymentTypeEnum.Spending);
        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.Account == _card));
    }

    [Fact]
    public async Task Upload_TransferDuplicateWithoutAccount_FillsAccountAndKeepsTransferType()
    {
        var existing = new Payment
        {
            PaymentId = 10, PaymentType = PaymentTypeEnum.Transfer, PaymentSum = -150m, PaymentDate = PaymentDate, Account = null
        };
        _paymentService.FindDuplicate(-150m, PaymentDate, 5, PaymentTypeEnum.Spending).Returns(existing);
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.Received(1).Update(Arg.Is<Payment[]>(ps =>
            ps.Length == 1 && ps[0] == existing && ps[0].Account == _card && ps[0].PaymentType == PaymentTypeEnum.Transfer));
    }

    [Fact]
    public async Task Upload_TransferDuplicateOnSameAccount_SkipsWithoutUpdate()
    {
        var existing = new Payment
        {
            PaymentId = 10, PaymentType = PaymentTypeEnum.Transfer, PaymentSum = 150m, PaymentDate = PaymentDate, Account = _card
        };
        _paymentService.FindDuplicate(150m, PaymentDate, 5, PaymentTypeEnum.Income).Returns(existing);
        SetRows(new ExcelPayment(PaymentDate, 150m, "Магазин", null, "OK", "Пополнение", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.DidNotReceive().Create(Arg.Any<Payment>());
        await _paymentService.DidNotReceiveWithAnyArgs().Update();
        Assert.Equal(PaymentTypeEnum.Transfer, existing.PaymentType);
    }

    [Fact]
    public async Task Upload_SameRowTwiceInFile_SecondRowSkipped()
    {
        // Вторая строка находит первую, созданную в той же транзакции, и тихо пропускается
        var created = new Payment { PaymentId = 100, PaymentSum = -150m, PaymentDate = PaymentDate, Account = _card };
        _paymentService.Create(Arg.Any<Payment>()).Returns(100);
        _paymentService.FindDuplicate(-150m, PaymentDate, 5, PaymentTypeEnum.Spending).Returns((Payment?)null, created);
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.Received(1).Create(Arg.Any<Payment>());
        await _paymentService.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Upload_AllRowsImported_CommitsSingleTransaction()
    {
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate.AddDays(1), -200m, "Магазин", null, "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _unitOfWork.Received(1).BeginTransaction(Arg.Any<CancellationToken>());
        await _transaction.Received(1).Commit(Arg.Any<CancellationToken>());
        await _transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Upload_ErrorOnSecondRow_DoesNotCommitAndDisposesTransaction()
    {
        var failingDate = PaymentDate.AddDays(1);
        _paymentService.FindDuplicate(-200m, failingDate, 5, PaymentTypeEnum.Spending).ThrowsAsync(new InvalidOperationException("ошибка БД"));
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"),
            new ExcelPayment(failingDate, -200m, "Магазин", null, "OK", "Списание", "Карта"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.Upload("file.xlsx", new MemoryStream()));

        await _paymentService.Received(1).Create(Arg.Any<Payment>());
        await _transaction.DidNotReceiveWithAnyArgs().Commit();
        await _transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Upload_Cancelled_DoesNotImportOrCommit()
    {
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"));
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
        _paymentService.FindDuplicate(-200m, PaymentDate, 5, PaymentTypeEnum.Spending).Returns(existingWithoutAccount);
        _paymentService.FindDuplicate(-300m, PaymentDate, 5, PaymentTypeEnum.Spending).Returns(existingWithAccount);
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate, -200m, "Магазин", null, "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate, -300m, "Магазин", null, "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal(
            "Импорт файла file.xlsx: строк 3, создано платежей 1, дозаполнен счёт 1, пропущено дублей 1",
            record.Message);
    }

    [Fact]
    public async Task Upload_Parsed_ClosesStream()
    {
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", "Карта"));
        var stream = new MemoryStream();

        await _service.Upload("file.xlsx", stream);

        Assert.False(stream.CanRead);
    }

    [Fact]
    public async Task Upload_ParseError_ClosesStreamAndPropagatesException()
    {
        _fileFormat.GetFileNodes(Arg.Any<Stream>()).Throws(new ValidationException("Строка 2: некорректная сумма"));
        var stream = new MemoryStream();

        await Assert.ThrowsAsync<ValidationException>(() => _service.Upload("file.csv", stream));

        Assert.False(stream.CanRead);
        await _unitOfWork.DidNotReceiveWithAnyArgs().BeginTransaction();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Upload_RowWithoutAccountName_CreatesPaymentWithoutAccount(string? accountName)
    {
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "Списание", accountName));

        await _service.Upload("file.csv", new MemoryStream());

        await _accountService.DidNotReceiveWithAnyArgs().GetByExcelName(default!);
        await _accountService.DidNotReceiveWithAnyArgs().Create(default!);
        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.Account == null && p.PaymentSource == _shop));
    }

    [Fact]
    public async Task Upload_DuplicateWithoutAccountAndRowWithoutAccount_SkipsRow()
    {
        var existing = new Payment { PaymentId = 10, PaymentSum = -150m, PaymentDate = PaymentDate, Account = null };
        _paymentService.FindDuplicate(-150m, PaymentDate, null, PaymentTypeEnum.Spending).Returns(existing);
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", null, "OK", "", null));

        await _service.Upload("file.csv", new MemoryStream());

        await _paymentService.DidNotReceive().Create(Arg.Any<Payment>());
        await _paymentService.DidNotReceiveWithAnyArgs().Update();
        Assert.Null(existing.Account);
    }

    [Theory]
    [InlineData(-150, PaymentTypeEnum.Spending)]
    [InlineData(300, PaymentTypeEnum.Income)]
    public async Task Upload_TypeNotSpendingOrIncome_TypeBySumSign(decimal sum, PaymentTypeEnum expectedType)
    {
        SetRows(new ExcelPayment(PaymentDate, sum, "Магазин", null, "OK", "Перевод", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.PaymentType == expectedType && p.PaymentSum == sum));
    }

    [Fact]
    public async Task Upload_SpendingTypeWithPositiveSum_TypeFromFileAndSumAsIs()
    {
        // Тип из файла приоритетнее знака суммы; знак суммы при импорте не меняется — его приводит NormalizeSumSign при чтении.
        SetRows(new ExcelPayment(PaymentDate, 150m, "Магазин", null, "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.PaymentType == PaymentTypeEnum.Spending && p.PaymentSum == 150m));
        await _paymentService.Received(1).FindDuplicate(150m, PaymentDate, 5, PaymentTypeEnum.Spending);
    }

    [Fact]
    public async Task Upload_RowWithoutSourceName_ThrowsArgumentNullExceptionWithoutCommit()
    {
        // Фиксирует текущее поведение: пустая ячейка источника в xlsx даёт SourceName = null, и импорт падает
        // необработанным исключением (500) вместо ValidationException — см. «Вне объёма» в plan.md задачи 0023.
        SetRows(new ExcelPayment(PaymentDate, -150m, null!, null, "OK", "Списание", "Карта"));

        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.Upload("file.xlsx", new MemoryStream()));

        await _paymentService.DidNotReceive().Create(Arg.Any<Payment>());
        await _transaction.DidNotReceiveWithAnyArgs().Commit();
    }

    [Fact]
    public async Task Upload_RowWithEmptySourceName_CreatesSourceWithEmptyName()
    {
        // Фиксирует текущее поведение: пустое имя источника (пустая колонка CSV) не отклоняется — создаётся
        // источник с пустым именем (см. «Вне объёма» в plan.md задачи 0023).
        var emptySource = new PaymentSource(9, "", null, null, Now, null);
        _paymentSourceService.Create(Arg.Any<PaymentSource>()).Returns(9);
        _paymentSourceService.GetById(9).Returns(emptySource);
        SetRows(new ExcelPayment(PaymentDate, -150m, "", null, "OK", "", null));

        await _service.Upload("file.csv", new MemoryStream());

        await _paymentSourceService.Received(1).Create(Arg.Is<PaymentSource>(s => s.PaymentSourceName == ""));
        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.PaymentSource == emptySource));
    }

    [Fact]
    public async Task Upload_KnownAccountNumber_UsesAccountFoundByNumber()
    {
        var byNumber = new Account(7, "Карта", "Карта", Now, null) { AccountNumber = "40817" };
        _accountService.GetByAccountNumber("40817").Returns(byNumber);
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", " 40817 ", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _accountService.DidNotReceiveWithAnyArgs().GetByExcelName(default!);
        await _accountService.DidNotReceiveWithAnyArgs().GetWithoutNumberByExcelName(default!);
        await _accountService.DidNotReceiveWithAnyArgs().Create(default!);
        await _accountService.DidNotReceiveWithAnyArgs().Update(default!);
        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.Account == byNumber));
    }

    [Fact]
    public async Task Upload_UnknownNumberAndLegacyAccountWithSameName_FillsNumber()
    {
        var legacy = new Account(5, "Карта", "Моя карта", Now, null);
        _accountService.GetWithoutNumberByExcelName("Карта").Returns(legacy);
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", "40817", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _accountService.Received(1).Update(Arg.Is<Account>(a => a.AccountId == 5 && a.AccountNumber == "40817" && a.DisplayName == "Моя карта"));
        await _accountService.DidNotReceiveWithAnyArgs().Create(default!);
        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.Account == legacy));
    }

    [Fact]
    public async Task Upload_UnknownNumberWithoutLegacyAccount_CreatesAccountWithNumber()
    {
        var created = new Account(8, "Карта", "Карта", Now, null) { AccountNumber = "40817" };
        _accountService.Create(Arg.Any<Account>()).Returns(8);
        _accountService.GetById(8).Returns(created);
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", "40817", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _accountService.Received(1).Create(Arg.Is<Account>(a =>
            a.ExcelAccountName == "Карта" && a.AccountNumber == "40817" && a.CreatedAt == Now));
        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.Account == created));
    }

    [Fact]
    public async Task Upload_NumberWithoutName_CreatesAccountNamedByNumber()
    {
        _accountService.Create(Arg.Any<Account>()).Returns(8);
        _accountService.GetById(8).Returns(new Account(8, "40817", "40817", Now, null));
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", "40817", "OK", "Списание", null));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _accountService.Received(1).GetWithoutNumberByExcelName("40817");
        await _accountService.Received(1).Create(Arg.Is<Account>(a => a.ExcelAccountName == "40817" && a.AccountNumber == "40817"));
    }

    [Fact]
    public async Task Upload_SameNumberInSeveralRows_LoadsAccountOnce()
    {
        _accountService.GetByAccountNumber("40817").Returns(_card);
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", "40817", "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate.AddDays(1), -200m, "Магазин", "40817", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _accountService.Received(1).GetByAccountNumber("40817");
        await _paymentService.Received(2).Create(Arg.Is<Payment>(p => p.Account == _card));
    }

    [Fact]
    public async Task Upload_SameNameDifferentNumbers_UsesDifferentAccounts()
    {
        var first = new Account(7, "Карта", "Карта", Now, null) { AccountNumber = "111" };
        var second = new Account(8, "Карта", "Карта", Now, null) { AccountNumber = "222" };
        _accountService.GetByAccountNumber("111").Returns(first);
        _accountService.GetByAccountNumber("222").Returns(second);
        SetRows(
            new ExcelPayment(PaymentDate, -150m, "Магазин", "111", "OK", "Списание", "Карта"),
            new ExcelPayment(PaymentDate, -200m, "Магазин", "222", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.PaymentSum == -150m && p.Account == first));
        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.PaymentSum == -200m && p.Account == second));
    }

    [Fact]
    public async Task Upload_RowWithoutNumber_FindsAccountByName()
    {
        SetRows(new ExcelPayment(PaymentDate, -150m, "Магазин", "  ", "OK", "Списание", "Карта"));

        await _service.Upload("file.xlsx", new MemoryStream());

        await _accountService.DidNotReceiveWithAnyArgs().GetByAccountNumber(default!);
        await _accountService.Received(1).GetByExcelName("Карта");
        await _paymentService.Received(1).Create(Arg.Is<Payment>(p => p.Account == _card));
    }

    private void SetRows(params ExcelPayment[] rows) =>
        _fileFormat.GetFileNodes(Arg.Any<Stream>()).Returns(rows.ToList());
}
