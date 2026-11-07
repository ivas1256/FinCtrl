using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Common.Application;
using FinCtrl.Domain.PaymentsImport;
using OfficeOpenXml;

namespace FinCtrl.Infrastructure.PaymentsImport
{
    internal class XlsxFileFormat : IFileFormat
    {
        public XlsxFileFormat()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<ExcelPayment> GetFileNodes(Stream fileStream)
        {
            var excelRows = new List<ExcelPayment>();
            using var xlsx = OpenPackage(fileStream);

            var ws = xlsx.Workbook.Worksheets.FirstOrDefault()
                ?? throw new ValidationException("В xlsx-файле нет листов");
            var row = 2;
            while (string.IsNullOrWhiteSpace(ws.Cells[row, 1].GetValue<string?>()) is false)
            {
                excelRows.Add(new ExcelPayment(
                               date: GetCellValue<DateTime>(ws, row, 1, "дата операции")
                                , sum: GetCellValue<decimal>(ws, row, 8, "сумма операции")
                                , sourceName: ws.Cells[row, 7].GetValue<string>()
                                , accountNumber: ws.Cells[row, 4].GetValue<string?>()
                                , status: ws.Cells[row, 10].GetValue<string>()
                                , type: ws.Cells[row, 13].GetValue<string>()
                                , accountName: ws.Cells[row, 3].GetValue<string>()
                                ));

                row++;
            }

            return excelRows;
        }

        private static ExcelPackage OpenPackage(Stream fileStream)
        {
            try
            {
                return new ExcelPackage(fileStream);
            }
            catch (InvalidDataException ex)
            {
                throw new ValidationException("Файл не является xlsx-документом или повреждён", ex);
            }
        }

        // EPPlus бросает FormatException/InvalidCastException на значении, которое не приводится к типу колонки.
        private static T GetCellValue<T>(ExcelWorksheet ws, int row, int column, string columnTitle)
        {
            try
            {
                return ws.Cells[row, column].GetValue<T>();
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException)
            {
                throw new ValidationException(
                    $"Строка {row}: некорректная {columnTitle} «{ws.Cells[row, column].Text}»", ex);
            }
        }
    }
}
