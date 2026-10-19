using CsvHelper;
using CsvHelper.Configuration;
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
                        fields[3], ""
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

    struct Test : IDisposable
    {
        private bool _flag;

        public void Dispose() { 
            _flag = true;
        }

        public bool GetDispose() => _flag;

        static void Main(string[] args) { 
            var d = new Test();
            using (d)
            {
                Console.WriteLine(d.GetDispose());
            }

            Console.WriteLine(d.GetDispose());
        }
    }

}
