using FinCtrl.Application.Accounts;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport;
using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Domain;
using FinCtrl.Domain.PaymentsImport;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

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
    private readonly PaymentsImportService _service;

    private readonly PaymentSource _shop = new(1, "Магазин", null, null, Now, null);
    private readonly Account _card = new(5, "Карта", "Карта", Now, null);

    public PaymentsImportServiceTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        timeProvider.SetUtcNow(new DateTimeOffset(Now, TimeSpan.Zero));

        _fileFormatFactory.Create(Arg.Any<string>()).Returns(_fileFormat);

        _paymentSourceService.Exists("Магазин").Returns(true);
        _paymentSourceService.GetByName("Магазин").Returns(_shop);
        _accountService.Exists("Карта").Returns(true);
        _accountService.GetByExcelName("Карта").Returns(_card);

        _service = new PaymentsImportService(_paymentSourceService, _accountService, _paymentService, _fileFormatFactory, timeProvider);
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
        _paymentSourceService.Exists("Кафе").Returns(false);
        _paymentSourceService.Create(Arg.Any<PaymentSource>()).Returns(2);
        _paymentSourceService.GetById(2).Returns(newSource);
        _accountService.Exists("Счёт").Returns(false);
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

    private void SetRows(params ExcelPayment[] rows) =>
        _fileFormat.GetFileNodes(Arg.Any<Stream>()).Returns(rows.ToList());
}
