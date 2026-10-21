using FinCtrl.Domain.PaymentsImport;

namespace FinCtrl.Application.PaymentsImport.FileFormats;

/// <summary>Формат банковского файла: разбор выгрузки в строки импорта.</summary>
public interface IFileFormat
{
    /// <summary>Читает строки операций из файла.</summary>
    List<ExcelPayment> GetFileNodes(Stream fileStream);
}
