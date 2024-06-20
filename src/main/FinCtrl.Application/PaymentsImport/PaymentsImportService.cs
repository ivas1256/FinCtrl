using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport.Abstractions;
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

namespace FinCtrl.Application.PaymentsImport
{
    internal class PaymentsImportService : IPaymentsImportService
    {
        private readonly IPaymentSourceService _paymentSourceService;
        private readonly IPaymentService _paymentService;

        public PaymentsImportService(IPaymentSourceService paymentSourceService, IPaymentService paymentService)
        {
            _paymentSourceService = paymentSourceService;
            _paymentService = paymentService;

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        }

        public void Upload(string fileName, Stream fileStream)
        {
            try
            {
                IFileFormat fileFormat = null;
                var ext = Path.GetExtension(fileName);
                if (ext == XlsxFileFormat.Extension)
                    fileFormat = new XlsxFileFormat();
                if (ext == CsvFileFormat.Extension)
                    fileFormat = new CsvFileFormat();

                var rows = fileFormat.GetFileNodes(fileStream)
                    .Where(x => x.Status == "OK").ToList();
                foreach (var row in rows)
                {
                    var source = GetOrCreatePaymentSource(row.SourceName);

                    if (_paymentService.Exists(row.Sum, row.Date) is false)
                    {
                        _paymentService.Create(Payment.FromExcelPayment(row, source));
                    }
                }
            }
            finally
            {
                fileStream.Close();
            }
        }

        private PaymentSource GetOrCreatePaymentSource(string name)
        {
            if (_paymentSourceService.Exists(name) is false)
            {
                var id = _paymentSourceService.Create(name);
                return _paymentSourceService.GetById(id);
            }
            else
            {
                return _paymentSourceService.GetByName(name);
            }
        }
    }
}
