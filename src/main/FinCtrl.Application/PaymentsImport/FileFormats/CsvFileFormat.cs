using CsvHelper;
using CsvHelper.Configuration;
using FinCtrl.Application.PaymentsImport.Abstractions;
using FinCtrl.Domain.PaymentsImport;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FinCtrl.Application.PaymentsImport.FileFormats
{
    public class CsvFileFormat : IFileFormat
    {
        public static string Extension => ".csv";

        public List<ExcelPayment> GetFileNodes(Stream fileStream)
        {
            var records = new List<ExcelPayment>();
            using (TextFieldParser parser = new TextFieldParser(fileStream, Encoding.GetEncoding("Windows-1251")))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.HasFieldsEnclosedInQuotes = true;
                parser.SetDelimiters(";");
                while (!parser.EndOfData)
                {
                    //Process row
                    string[] fields = parser.ReadFields();
                    if (parser.LineNumber == 2)
                        continue;

                    records.Add(new ExcelPayment
                    (
                        DateTime.Parse(fields[0]),
                        decimal.Parse(fields[6]),
                        fields[11],
                        fields[2],
                        fields[3]
                    ));
                }
            }

            return records;
        }
    }

    class CsvMap : ClassMap<ExcelPayment>
    {
        public CsvMap()
        {
            Map(x => x.Date).Index(1);
            Map(x => x.Sum).Index(7);
            Map(x => x.SourceName).Index(12);
            Map(x => x.InvoiceNumber).Index(3);
            Map(x => x.Status).Index(4);
        }
    }
}
