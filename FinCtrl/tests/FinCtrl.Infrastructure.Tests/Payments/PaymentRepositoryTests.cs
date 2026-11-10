using System.Reflection;
using System.Text.RegularExpressions;
using FinCtrl.Infrastructure.Payments;

namespace FinCtrl.Infrastructure.Tests.Payments;

public class PaymentRepositoryTests
{
    [Fact]
    public void SelectPayment_ColumnAliases_MatchPaymentDtoProperties()
    {
        var columns = GetSelectColumnNames(PaymentRepository.SelectPayment);
        var properties = typeof(PaymentDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToList();

        // Колонка без свойства молча теряется Dapper'ом (так было с ParentCategoryCategoryId),
        // свойство без колонки всегда остаётся пустым.
        Assert.Empty(columns.Except(properties));
        Assert.Empty(properties.Except(columns));
    }

    [Fact]
    public void SelectPayment_PaymentSourceCategoryParent_IsSelected()
    {
        var columns = GetSelectColumnNames(PaymentRepository.SelectPayment);

        Assert.Contains(nameof(PaymentDto.PaymentSourceCategory_ParentCategoryId), columns);
    }

    // Имена колонок результата: алиас после AS (или после пробела), иначе имя колонки после точки.
    // Список колонок заканчивается на FROM Payments (подзапросы в колонках, например EXISTS, своим FROM его не обрывают);
    // подзапрос в колонке не должен содержать запятых — разбиение идёт по ним.
    private static List<string> GetSelectColumnNames(string sql)
    {
        var match = Regex.Match(sql, @"SELECT(?<cols>.*?)\bFROM\s+Payments\b", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        Assert.True(match.Success);

        return match.Groups["cols"].Value
            .Split(',')
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Select(x => x.Split(new[] { ' ', '\t', '\r', '\n', '.' }, StringSplitOptions.RemoveEmptyEntries).Last())
            .ToList();
    }
}
