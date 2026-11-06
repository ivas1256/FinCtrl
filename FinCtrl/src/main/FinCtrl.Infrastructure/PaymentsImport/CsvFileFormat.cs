using System.Globalization;
using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Common.Application;
using FinCtrl.Domain.PaymentsImport;
using Microsoft.VisualBasic.FileIO;

namespace FinCtrl.Infrastructure.PaymentsImport
{
    internal class CsvFileFormat : IFileFormat
    {
        private const int DateColumn = 0;
        private const int StatusColumn = 3;
        private const int SumColumn = 6;
        private const int SourceNameColumn = 11;
        private const int MinColumnCount = SourceNameColumn + 1;

        // Банковская выгрузка — в русском формате (01.10.2025 12:30:00, -1 234,50), но разбор не должен зависеть
        // от культуры сервера; поэтому культура задаётся явно, а ISO-даты и суммы с точкой тоже принимаются.
        private static readonly CultureInfo BankCulture = CultureInfo.GetCultureInfo("ru-RU");

        public List<ExcelPayment> GetFileNodes(Stream fileStream)
        {
            var records = new List<ExcelPayment>();
            using var parser = new TextFieldParser(fileStream);
            parser.TextFieldType = FieldType.Delimited;
            parser.HasFieldsEnclosedInQuotes = true;
            parser.SetDelimiters(";");

            // Номер строки считаем сами: LineNumber у TextFieldParser после последней строки без перевода строки равен -1.
            var lineNumber = 0;
            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields();
                lineNumber++;

                // Первая строка файла — заголовок
                if (fields is null || lineNumber == 1)
                    continue;

                records.Add(ParseRow(fields, lineNumber));
            }

            return records;
        }

        private static ExcelPayment ParseRow(string[] fields, int lineNumber)
        {
            if (fields.Length < MinColumnCount)
                throw new ValidationException($"Строка {lineNumber}: ожидается не меньше {MinColumnCount} колонок, найдено {fields.Length}");

            return new ExcelPayment(
                ParseDate(fields[DateColumn], lineNumber),
                ParseSum(fields[SumColumn], lineNumber),
                fields[SourceNameColumn],
                accountNumber: null,
                fields[StatusColumn],
                "");
        }

        private static DateTime ParseDate(string value, int lineNumber)
        {
            if (DateTime.TryParse(value, BankCulture, DateTimeStyles.None, out var date)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return date;

            throw new ValidationException($"Строка {lineNumber}: некорректная дата операции «{value}»");
        }

        private static decimal ParseSum(string value, int lineNumber)
        {
            // Пробелы — разделители разрядов (в т.ч. неразрывные), запятая и точка — десятичный разделитель.
            var normalized = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var sum))
                return sum;

            throw new ValidationException($"Строка {lineNumber}: некорректная сумма операции «{value}»");
        }
    }
}
