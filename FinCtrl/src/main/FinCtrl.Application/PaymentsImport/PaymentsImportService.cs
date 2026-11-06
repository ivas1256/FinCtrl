using FinCtrl.Application.Accounts;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Application.PaymentSources.Classification;
using FinCtrl.Common;
using FinCtrl.Common.Application;
using FinCtrl.Domain;
using FinCtrl.Domain.PaymentsImport;
using Microsoft.Extensions.Logging;

namespace FinCtrl.Application.PaymentsImport
{
    internal class PaymentsImportService : IPaymentsImportService
    {
        private readonly IPaymentSourceService _paymentSourceService;
        private readonly IAccountService _accountService;
        private readonly IPaymentService _paymentService;
        private readonly IFileFormatFactory _fileFormatFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentSourceClassificationService _classificationService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<PaymentsImportService> _logger;

        public PaymentsImportService(IPaymentSourceService paymentSourceService,
                                     IAccountService accountService,
                                     IPaymentService paymentService,
                                     IFileFormatFactory fileFormatFactory,
                                     IUnitOfWork unitOfWork,
                                     IPaymentSourceClassificationService classificationService,
                                     TimeProvider timeProvider,
                                     ILogger<PaymentsImportService> logger)
        {
            _paymentSourceService = paymentSourceService;
            _accountService = accountService;
            _paymentService = paymentService;
            _fileFormatFactory = fileFormatFactory;
            _unitOfWork = unitOfWork;
            _classificationService = classificationService;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<PaymentsImportResult> Upload(string fileName, Stream fileStream, CancellationToken cancellationToken = default)
        {
            List<ExcelPayment> rows;
            try
            {
                var fileFormat = _fileFormatFactory.Create(Path.GetExtension(fileName));
                rows = fileFormat.GetFileNodes(fileStream).ToList();
            }
            finally
            {
                fileStream.Close();
            }

            // Пустой файл или файл только с заголовком — скорее всего, выбран не тот файл; молча «успешно» не отвечаем.
            if (rows.Count == 0)
                throw new ValidationException($"В файле «{fileName}» нет операций для импорта");

            var summary = new ImportSummary();
            var context = new ImportContext();

            // Весь файл — одна транзакция: ошибка на любой строке откатывает и платежи, и созданные источники/счета.
            await using (var transaction = await _unitOfWork.BeginTransaction(cancellationToken))
            {
                foreach (var row in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ImportRow(row, context, summary);
                }

                await transaction.Commit(cancellationToken);
            }

            _logger.LogInformation(
                "Импорт файла {FileName}: строк {Rows}, создано платежей {Created}, дозаполнен счёт {AccountFilled}, пропущено дублей {Skipped}",
                fileName, rows.Count, summary.Created, summary.AccountFilled, summary.Skipped);

            // Классификация — после коммита: запрос к LLM долгий, держать транзакцию ради него незачем,
            // а его ошибка не должна откатывать уже загруженные платежи.
            var classificationError = await _classificationService.Classify(context.NewSources, cancellationToken);

            return new PaymentsImportResult(context.NewPayments, context.NewSources, classificationError);
        }

        private async Task ImportRow(ExcelPayment row, ImportContext context, ImportSummary summary)
        {
            var source = await GetOrCreatePaymentSource(row.SourceName, context);
            var account = string.IsNullOrWhiteSpace(row.AccountName)
                ? null
                : await GetOrCreateAccount(row.AccountName, context);

            var payment = Payment.FromExcelPayment(row, source, account, _timeProvider.GetLocalNow().DateTime);

            if (row.Type == "Списание")
            {
                payment.PaymentType = PaymentTypeEnum.Spending;
            }
            else if (row.Type == "Пополнение")
            {
                payment.PaymentType = PaymentTypeEnum.Income;
            }

            var existingPayment = await _paymentService.FindDuplicate(payment.PaymentSum, payment.PaymentDate, account?.AccountId, payment.PaymentType);
            if (existingPayment is null)
            {
                payment.PaymentId = await _paymentService.Create(payment);
                context.NewPayments.Add(payment);
                summary.Created++;
            }
            else if (existingPayment.Account is null && account is not null)
            {
                // Дозаполнение счёта у ранее импортированных платежей при повторной загрузке файла
                existingPayment.Account = account;
                await _paymentService.Update(existingPayment);
                summary.AccountFilled++;
            }
            else
            {
                summary.Skipped++;
            }
        }

        // Источники и счета повторяются из строки в строку — кэш на время файла избавляет от запросов на каждую строку.
        private async Task<PaymentSource> GetOrCreatePaymentSource(string name, ImportContext context)
        {
            if (context.Sources.TryGetValue(name, out var cached))
            {
                return cached;
            }

            var source = await _paymentSourceService.GetByName(name);
            if (source is null)
            {
                var id = await _paymentSourceService.Create(PaymentSource.CreateNew(name, _timeProvider.GetLocalNow().DateTime));
                source = await _paymentSourceService.GetById(id);
                context.NewSources.Add(source);
            }

            context.Sources[name] = source;
            return source;
        }

        private async Task<Account> GetOrCreateAccount(string excelAccountName, ImportContext context)
        {
            if (context.Accounts.TryGetValue(excelAccountName, out var cached))
            {
                return cached;
            }

            var account = await _accountService.GetByExcelName(excelAccountName);
            if (account is null)
            {
                var id = await _accountService.Create(Account.CreateNew(excelAccountName, _timeProvider.GetLocalNow().DateTime));
                account = await _accountService.GetById(id);
            }

            context.Accounts[excelAccountName] = account;
            return account;
        }

        // Сравнение Ordinal: по-другому написанное имя просто уйдёт в БД, где сработает её collation — как и до кэша.
        private sealed class ImportContext
        {
            public Dictionary<string, PaymentSource> Sources { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, Account> Accounts { get; } = new(StringComparer.Ordinal);
            public List<PaymentSource> NewSources { get; } = new();
            public List<Payment> NewPayments { get; } = new();
        }

        private sealed class ImportSummary
        {
            public int Created { get; set; }
            public int AccountFilled { get; set; }
            public int Skipped { get; set; }
        }
    }
}
