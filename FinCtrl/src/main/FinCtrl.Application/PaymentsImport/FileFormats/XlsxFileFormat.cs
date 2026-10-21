using FinCtrl.Domain.PaymentsImport;
using OfficeOpenXml;

namespace FinCtrl.Application.PaymentsImport.FileFormats
{
    public class XlsxFileFormat : IFileFormat
    {
        public XlsxFileFormat()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<ExcelPayment> GetFileNodes(Stream fileStream)
        {
            var excelRows = new List<ExcelPayment>();
            using (var xlsx = new ExcelPackage(fileStream))
            {
                var ws = xlsx.Workbook.Worksheets.First();
                var row = 2;
                while (string.IsNullOrWhiteSpace(ws.Cells[row, 1].GetValue<string?>()) is false)
                {
                    excelRows.Add(new ExcelPayment(
                                   date: ws.Cells[row, 1].GetValue<DateTime>()
                                    , sum: ws.Cells[row, 8].GetValue<decimal>()
                                    , sourceName: ws.Cells[row, 7].GetValue<string>()
                                    , invoiceNumber: ws.Cells[row, 4].GetValue<string>()
                                    , status: ws.Cells[row, 10].GetValue<string>()
                                    , type: ws.Cells[row, 13].GetValue<string>()
                                    , accountName: ws.Cells[row, 3].GetValue<string>()
                                    ));

                    row++;
                }
            }

            return excelRows;
        }
    }
}
