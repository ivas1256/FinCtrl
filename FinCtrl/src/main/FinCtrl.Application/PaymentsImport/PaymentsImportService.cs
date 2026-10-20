using FinCtrl.Application.Accounts;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Domain;
using FinCtrl.Domain.PaymentsImport;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FinCtrl.Application.PaymentsImport
{
    internal class PaymentsImportService : IPaymentsImportService
    {
        private readonly IPaymentSourceService _paymentSourceService;
        private readonly IAccountService _accountService;
        private readonly IPaymentService _paymentService;
        private readonly FileFormatFactory _fileFormatFactory;
        private readonly TimeProvider _timeProvider;

        public PaymentsImportService(IPaymentSourceService paymentSourceService,
                                     IAccountService accountService,
                                     IPaymentService paymentService,
                                     FileFormatFactory fileFormatFactory,
                                     TimeProvider timeProvider)
        {
            _paymentSourceService = paymentSourceService;
            _accountService = accountService;
            _paymentService = paymentService;
            _fileFormatFactory = fileFormatFactory;
            _timeProvider = timeProvider;

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        }

        public async Task Upload(string fileName, Stream fileStream)
        {
            try
            {
                var fileFormat = _fileFormatFactory.Create(Path.GetExtension(fileName));

                var rows = fileFormat
                    .GetFileNodes(fileStream)
                    //.Where(x => x.Status == "OK")
                    .ToList();

                foreach (var row in rows)
                {
                    var source = await GetOrCreatePaymentSource(row.SourceName);
                    var account = string.IsNullOrWhiteSpace(row.AccountName)
                        ? null
                        : await GetOrCreateAccount(row.AccountName);

                    var payment = Payment.FromExcelPayment(row, source, account, _timeProvider.GetLocalNow().DateTime);

                    if(row.Type == "Списание")
                    {
                        payment.PaymentType = PaymentTypeEnum.Spending;
                    }
                    else if (row.Type == "Пополнение")
                    {
                        payment.PaymentType = PaymentTypeEnum.Income;
                    }

                    var existingPayment = await _paymentService.FindDuplicate(payment.PaymentSum, payment.PaymentDate);
                    if (existingPayment is null)
                    {
                        await _paymentService.Create(payment);
                    }
                    else if (existingPayment.Account is null && account is not null)
                    {
                        // Дозаполнение счёта у ранее импортированных платежей при повторной загрузке файла
                        existingPayment.Account = account;
                        await _paymentService.Update(existingPayment);
                    }
                }
            }
            finally
            {
                fileStream.Close();
            }
        }

        private async Task<PaymentSource> GetOrCreatePaymentSource(string name)
        {
            if (await _paymentSourceService.Exists(name) is false)
            {
                var id = await _paymentSourceService.Create(PaymentSource.CreateNew(name, _timeProvider.GetLocalNow().DateTime));
                return await _paymentSourceService.GetById(id)
                    ?? throw new InvalidOperationException($"Источник платежа {id} не найден после создания");
            }
            else
            {
                return await _paymentSourceService.GetByName(name)
                    ?? throw new InvalidOperationException($"Источник платежа \"{name}\" не найден");
            }
        }

        private async Task<Account> GetOrCreateAccount(string excelAccountName)
        {
            if (await _accountService.Exists(excelAccountName) is false)
            {
                var id = await _accountService.Create(Account.CreateNew(excelAccountName, _timeProvider.GetLocalNow().DateTime));
                return await _accountService.GetById(id)
                    ?? throw new InvalidOperationException($"Счёт {id} не найден после создания");
            }
            else
            {
                return await _accountService.GetByExcelName(excelAccountName)
                    ?? throw new InvalidOperationException($"Счёт \"{excelAccountName}\" не найден");
            }
        }
    }
}
