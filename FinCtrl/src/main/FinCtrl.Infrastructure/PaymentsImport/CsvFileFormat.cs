using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Domain.PaymentsImport;
using Microsoft.VisualBasic.FileIO;

namespace FinCtrl.Infrastructure.PaymentsImport
{
    internal class CsvFileFormat : IFileFormat
    {
        public List<ExcelPayment> GetFileNodes(Stream fileStream)
        {
            var records = new List<ExcelPayment>();
            using (TextFieldParser parser = new TextFieldParser(fileStream))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.HasFieldsEnclosedInQuotes = true;
                parser.SetDelimiters(";");
                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    // Первая строка файла — заголовок
                    if (fields is null || parser.LineNumber == 2)
                        continue;

                    records.Add(new ExcelPayment
                    (
                        DateTime.Parse(fields[0]),
                        decimal.Parse(fields[6]),
                        fields[11],
                        fields[2],
                        fields[3], ""
                    ));
                }
            }

            return records;
        }
    }
}
