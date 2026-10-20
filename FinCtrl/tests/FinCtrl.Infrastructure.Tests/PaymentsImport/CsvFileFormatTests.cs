using System.Globalization;
using System.Text;
using FinCtrl.Common.Application;
using FinCtrl.Infrastructure.PaymentsImport;

namespace FinCtrl.Infrastructure.Tests.PaymentsImport;

public class CsvFileFormatTests
{
    // Колонки: 0 — дата, 2 — номер документа, 3 — статус, 6 — сумма, 11 — источник
    private const string Header = "Дата;1;Номер;Статус;4;5;Сумма;7;8;9;10;Источник";

    [Fact]
    public void GetFileNodes_FileWithHeader_SkipsHeaderAndMapsColumns()
    {
        var csv = string.Join("\n",
            Header,
            "2025-10-01;x;INV-1;OK;x;x;-150;x;x;x;x;\"Магазин; у дома\"",
            "2025-10-02;x;INV-2;FAILED;x;x;300;x;x;x;x;Кафе");

        var rows = new CsvFileFormat().GetFileNodes(ToStream(csv));

        Assert.Equal(2, rows.Count);
        Assert.Equal(new DateTime(2025, 10, 1), rows[0].Date);
        Assert.Equal(-150m, rows[0].Sum);
        Assert.Equal("Магазин; у дома", rows[0].SourceName);
        Assert.Equal("INV-1", rows[0].InvoiceNumber);
        Assert.Equal("OK", rows[0].Status);
        Assert.Equal("Кафе", rows[1].SourceName);
        Assert.Equal(300m, rows[1].Sum);
    }

    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    [InlineData("")]
    public void GetFileNodes_AnyCurrentCulture_ParsesDotAndCommaSumsAndBankDates(string cultureName)
    {
        var csv = string.Join("\n",
            Header,
            "01.10.2025 12:30:00;x;INV-1;OK;x;x;-55.5;x;x;x;x;Магазин",
            "02.10.2025;x;INV-2;OK;x;x;\"-1 234,50\";x;x;x;x;Кафе",
            "2025-10-03;x;INV-3;OK;x;x;300;x;x;x;x;Работа");

        var rows = WithCulture(cultureName, () => new CsvFileFormat().GetFileNodes(ToStream(csv)));

        Assert.Equal([-55.5m, -1234.5m, 300m], rows.Select(x => x.Sum));
        Assert.Equal(
            [new DateTime(2025, 10, 1, 12, 30, 0), new DateTime(2025, 10, 2), new DateTime(2025, 10, 3)],
            rows.Select(x => x.Date));
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void GetFileNodes_OnlyHeader_ReturnsEmptyList(string lineEnding)
    {
        var rows = new CsvFileFormat().GetFileNodes(ToStream(Header + lineEnding));

        Assert.Empty(rows);
    }

    [Fact]
    public void GetFileNodes_EmptyFile_ReturnsEmptyList()
    {
        Assert.Empty(new CsvFileFormat().GetFileNodes(ToStream("")));
    }

    [Theory]
    [InlineData("2025-10-01;x;INV-1;OK;x;x;сто;x;x;x;x;Магазин", "сумм")]
    [InlineData("вчера;x;INV-1;OK;x;x;-150;x;x;x;x;Магазин", "дат")]
    [InlineData("2025-10-01;x;INV-1;OK", "колонок")]
    public void GetFileNodes_InvalidRow_ThrowsValidationExceptionWithLineNumber(string row, string expectedText)
    {
        var csv = string.Join("\n", Header, "2025-10-01;x;INV-0;OK;x;x;-1;x;x;x;x;Кафе", row);

        var ex = Assert.Throws<ValidationException>(() => new CsvFileFormat().GetFileNodes(ToStream(csv)));

        Assert.Contains("строка 3", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(expectedText, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetFileNodes_AnyRow_TypeEmptyAndAccountNotFilled()
    {
        // В CSV-выписке нет колонок типа операции и счёта: тип определяется по знаку суммы, счёт не заполняется.
        var csv = string.Join("\n", Header, "2025-10-01;x;INV-1;Списание;x;x;-150;x;x;x;x;Магазин");

        var row = Assert.Single(new CsvFileFormat().GetFileNodes(ToStream(csv)));

        Assert.Equal(string.Empty, row.Type);
        Assert.Null(row.AccountName);
        Assert.Equal("Списание", row.Status);
    }

    [Fact]
    public void GetFileNodes_BlankLinesAndExtraColumns_BlankLinesSkippedExtraColumnsIgnored()
    {
        var csv = string.Join("\r\n",
            Header,
            "",
            "2025-10-01;x;INV-1;OK;x;x;-150;x;x;x;x;Магазин;лишняя;колонка",
            "",
            "2025-10-02;x;INV-2;OK;x;x;300;x;x;x;x;Кафе",
            "");

        var rows = new CsvFileFormat().GetFileNodes(ToStream(csv));

        Assert.Equal(["Магазин", "Кафе"], rows.Select(x => x.SourceName));
    }

    [Fact]
    public void GetFileNodes_Utf8WithBom_ParsesFirstRowAsHeader()
    {
        var csv = string.Join("\n", Header, "2025-10-01;x;INV-1;OK;x;x;-150;x;x;x;x;Магазин");
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();

        var row = Assert.Single(new CsvFileFormat().GetFileNodes(new MemoryStream(bytes)));

        Assert.Equal("Магазин", row.SourceName);
    }

    private static MemoryStream ToStream(string content) => new(Encoding.UTF8.GetBytes(content));

    private static T WithCulture<T>(string cultureName, Func<T> action)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(cultureName);
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
