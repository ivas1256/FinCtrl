using FinCtrl.DAL;
using FinCtrl.DAL.Implementation;
using FinCtrl.DAL.Models;
using OfficeOpenXml;

namespace FinCtrl.Backend.Core.RestAPI.BL.Implementation
{
    public class ExcelFileLoader
    {
        PaymentRepository paymentRepository;
        PaymentSourceRepository paymentSourceRepository;

        public ExcelFileLoader(PaymentRepository paymentRepository, PaymentSourceRepository paymentSourceRepository)
        {
            this.paymentRepository = paymentRepository;
            this.paymentSourceRepository = paymentSourceRepository;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public void Upload(Stream fileStream)
        {
            var rows = parseFile(fileStream);
            foreach (var row in rows)
            {
                var source = paymentSourceRepository.CreateOrGet(new PaymentSource() { PaymentSourceName = row.SourceName });

                if (!paymentRepository.Exists(new Payment(row.Date, row.Sum)))
                {
                    paymentRepository.CreateNoCommit(new Payment(
                        0,
                        row.Sum < 0 ? PaymentTypes.Spending : PaymentTypes.Income
                        , row.Sum
                        , row.Date
                        , source));
                }                
            }

            paymentRepository.Commit();
        }

        List<ExcelPayment> parseFile(Stream fileStream)
        {
            var excelRows = new List<ExcelPayment>();
            using (var xlsx = new ExcelPackage(fileStream))
            {
                var ws = xlsx.Workbook.Worksheets.First();
                var row = 2; 
                while (ws.Cells[row, 1].Value != null)
                {
                    excelRows.Add(new ExcelPayment(
                        ws.Cells[row, 1].GetValue<DateTime>()
                        , ws.Cells[row, 7].GetValue<decimal>()
                        , ws.Cells[row, 12].GetValue<string>()
                        , ws.Cells[row, 3].GetValue<string>()
                        ));

                    row++;
                }
            }

            fileStream.Close();
            return excelRows;
        }
    }
    struct ExcelPayment
    {
        public ExcelPayment(DateTime date, decimal sum, string sourceName, string invoiceNumber)
        {
            Date = date;
            Sum = sum;
            SourceName = sourceName;
            InvoiceNumber = invoiceNumber;
        }

        public DateTime Date { get; set; }
        public decimal Sum { get; set; }
        public string SourceName { get; set; }
        public string InvoiceNumber { get; set; }
    }
}
