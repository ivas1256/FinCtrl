using FinCtrl.Common.Application;
using FinCtrl.Infrastructure.PaymentsImport;
using OfficeOpenXml;

namespace FinCtrl.Infrastructure.Tests.PaymentsImport;

public class XlsxFileFormatTests
{
    [Fact]
    public void GetFileNodes_RowsUntilEmptyDate_MapsColumns()
    {
        var format = new XlsxFileFormat();
        using var file = CreateFile(
            (new DateTime(2025, 10, 1), "Карта", "INV-1", "Магазин", -150m, "OK", "Списание"),
            (new DateTime(2025, 10, 2), "Счёт", "INV-2", "Работа", 1000m, "OK", "Пополнение"));

        var rows = format.GetFileNodes(file);

        Assert.Equal(2, rows.Count);
        var first = rows[0];
        Assert.Equal(new DateTime(2025, 10, 1), first.Date);
        Assert.Equal("Карта", first.AccountName);
        Assert.Equal("INV-1", first.InvoiceNumber);
        Assert.Equal("Магазин", first.SourceName);
        Assert.Equal(-150m, first.Sum);
        Assert.Equal("OK", first.Status);
        Assert.Equal("Списание", first.Type);
        Assert.Equal("Пополнение", rows[1].Type);
    }

    [Fact]
    public void GetFileNodes_OnlyHeader_ReturnsEmptyList()
    {
        var format = new XlsxFileFormat();
        using var file = CreateFile();

        var rows = format.GetFileNodes(file);

        Assert.Empty(rows);
    }

    [Fact]
    public void GetFileNodes_NotXlsxContent_ThrowsValidationException()
    {
        var format = new XlsxFileFormat();
        using var file = new MemoryStream("это не xlsx"u8.ToArray());

        Assert.Throws<ValidationException>(() => format.GetFileNodes(file));
    }

    [Theory]
    [InlineData(1, "вчера", "дат")]
    [InlineData(8, "сто рублей", "сумм")]
    public void GetFileNodes_InvalidCellValue_ThrowsValidationExceptionWithRowNumber(int column, string value, string expectedText)
    {
        var format = new XlsxFileFormat();
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Выписка");
        ws.Cells[2, 1].Value = new DateTime(2025, 10, 1);
        ws.Cells[2, 8].Value = -150m;
        ws.Cells[2, column].Value = value;
        using var file = new MemoryStream(package.GetAsByteArray());

        var ex = Assert.Throws<ValidationException>(() => format.GetFileNodes(file));

        Assert.Contains("строка 2", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(expectedText, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetFileNodes_RowsAfterEmptyDate_Ignored()
    {
        var format = new XlsxFileFormat();
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Выписка");
        ws.Cells[2, 1].Value = new DateTime(2025, 10, 1);
        ws.Cells[2, 8].Value = -150m;
        ws.Cells[4, 1].Value = new DateTime(2025, 10, 3);
        ws.Cells[4, 8].Value = -300m;
        using var file = new MemoryStream(package.GetAsByteArray());

        var row = Assert.Single(format.GetFileNodes(file));

        Assert.Equal(-150m, row.Sum);
    }

    [Fact]
    public void GetFileNodes_EmptyTextCells_MappedToNull()
    {
        var format = new XlsxFileFormat();
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Выписка");
        ws.Cells[2, 1].Value = new DateTime(2025, 10, 1, 12, 30, 0);
        ws.Cells[2, 8].Value = -150m;
        using var file = new MemoryStream(package.GetAsByteArray());

        var row = Assert.Single(format.GetFileNodes(file));

        Assert.Equal(new DateTime(2025, 10, 1, 12, 30, 0), row.Date);
        Assert.Null(row.AccountName);
        Assert.Null(row.SourceName);
        Assert.Null(row.Type);
    }

    [Fact]
    public void GetFileNodes_SeveralSheets_ReadsOnlyFirst()
    {
        var format = new XlsxFileFormat();
        using var package = new ExcelPackage();
        var first = package.Workbook.Worksheets.Add("Выписка");
        first.Cells[2, 1].Value = new DateTime(2025, 10, 1);
        first.Cells[2, 8].Value = -150m;
        var second = package.Workbook.Worksheets.Add("Другой");
        second.Cells[2, 1].Value = new DateTime(2025, 10, 2);
        second.Cells[2, 8].Value = -300m;
        using var file = new MemoryStream(package.GetAsByteArray());

        var row = Assert.Single(format.GetFileNodes(file));

        Assert.Equal(-150m, row.Sum);
    }

    // Колонки выписки: 1 — дата, 3 — счёт, 4 — номер документа, 7 — источник, 8 — сумма, 10 — статус, 13 — тип
    private static MemoryStream CreateFile(
        params (DateTime Date, string Account, string Invoice, string Source, decimal Sum, string Status, string Type)[] rows)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Выписка");
        ws.Cells[1, 1].Value = "Дата";

        for (var i = 0; i < rows.Length; i++)
        {
            var r = i + 2;
            ws.Cells[r, 1].Value = rows[i].Date;
            ws.Cells[r, 3].Value = rows[i].Account;
            ws.Cells[r, 4].Value = rows[i].Invoice;
            ws.Cells[r, 7].Value = rows[i].Source;
            ws.Cells[r, 8].Value = rows[i].Sum;
            ws.Cells[r, 10].Value = rows[i].Status;
            ws.Cells[r, 13].Value = rows[i].Type;
        }

        return new MemoryStream(package.GetAsByteArray());
    }
}
