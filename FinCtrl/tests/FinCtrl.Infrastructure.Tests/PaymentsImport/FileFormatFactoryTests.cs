using FinCtrl.Common.Application;
using FinCtrl.Infrastructure.PaymentsImport;

namespace FinCtrl.Infrastructure.Tests.PaymentsImport;

public class FileFormatFactoryTests
{
    private readonly FileFormatFactory _factory = new();

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".XLSX")]
    public void Create_XlsxExtension_ReturnsXlsxFormat(string extension)
    {
        var format = _factory.Create(extension);

        Assert.IsType<XlsxFileFormat>(format);
    }

    [Theory]
    [InlineData(".csv")]
    [InlineData(".Csv")]
    public void Create_CsvExtension_ReturnsCsvFormat(string extension)
    {
        var format = _factory.Create(extension);

        Assert.IsType<CsvFileFormat>(format);
    }

    [Theory]
    [InlineData(".xls")]
    [InlineData("")]
    public void Create_UnknownExtension_ThrowsValidationException(string extension)
    {
        Assert.Throws<ValidationException>(() => _factory.Create(extension));
    }
}
