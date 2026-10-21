namespace FinCtrl.Application.PaymentsImport.FileFormats;

/// <summary>Выбирает формат банковского файла для импорта.</summary>
public interface IFileFormatFactory
{
    /// <summary>Формат по расширению файла (без учёта регистра).</summary>
    /// <param name="fileExtension">Расширение с точкой, например <c>.xlsx</c>.</param>
    /// <exception cref="FinCtrl.Common.Application.ValidationException">Формат не поддерживается.</exception>
    IFileFormat Create(string fileExtension);
}
