using System.Text;
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

    private static MemoryStream ToStream(string content) => new(Encoding.UTF8.GetBytes(content));
}
