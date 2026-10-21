using FinCtrl.Application.PaymentsImport.FileFormats;

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
                throw new NotImplementedException($"Неизвестный формат файла {fileExtension}");
        }
    }
}
