using FinCtrl.Application.PaymentsImport.Abstractions;
using FinCtrl.Domain.PaymentsImport;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FinCtrl.Application.PaymentsImport.FileFormats
{
    public class XlsxFileFormat : IFileFormat
    {
        public static string Extension => ".xlsx";

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
                while (ws.Cells[row, 1].Value != null)
                {
                    excelRows.Add(new ExcelPayment(
                        ws.Cells[row, 1].GetValue<DateTime>()
                        , ws.Cells[row, 7].GetValue<decimal>()
                        , ws.Cells[row, 12].GetValue<string>()
                        , ws.Cells[row, 3].GetValue<string>()
                        , ws.Cells[row, 4].GetValue<string>()
                        ));


                    row++;
                }
            }

            return excelRows;
        }
    }
}
