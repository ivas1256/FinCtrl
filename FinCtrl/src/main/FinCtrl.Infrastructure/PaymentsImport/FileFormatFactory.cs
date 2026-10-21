using FinCtrl.Application.PaymentsImport.FileFormats;
using FinCtrl.Common.Application;

namespace FinCtrl.Infrastructure.PaymentsImport;

internal class FileFormatFactory : IFileFormatFactory
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
                throw new ValidationException($"Неподдерживаемый формат файла «{fileExtension}»: поддерживаются .xlsx и .csv");
        }
    }
}
