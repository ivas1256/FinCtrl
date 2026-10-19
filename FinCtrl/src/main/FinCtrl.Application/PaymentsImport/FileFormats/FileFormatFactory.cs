using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Application.PaymentsImport.FileFormats
{
    internal class FileFormatFactory
    {
        public IFileFormat Create(string fileExtension)
        {
            switch (fileExtension.ToLower())
            {
                case ".xlsx":
                    return new XlsxFileFormat();
                case ".csv":
                    return new CsvFileFormat();
                default:
                    throw new NotImplementedException($"Неизвестный формат файла {fileExtension}");

            }
        }
    }
}
