using FinCtrl.Common;

namespace FinCtrl.Domain.Statistic;

/// <summary>Read-модель таблицы главной страницы: дерево категорий × периоды группировки.</summary>
public class PaymentsTable
{
    /// <summary>Периоды по возрастанию.</summary>
    public List<PaymentsTablePeriod> Periods { get; init; } = new();

    /// <summary>Корневые категории, затем «Без категории» и итоговая строка.</summary>
    public List<PaymentsTableNode> Tree { get; init; } = new();
}

/// <summary>Период таблицы: ключ колонки и диапазон дат (<c>To</c> — исключающая граница).</summary>
public class PaymentsTablePeriod
{
    public required string Key { get; init; }
    public required DateRange Range { get; init; }
}

/// <summary>Строка таблицы — категория с ячейками по ключам всех периодов и дочерними строками.</summary>
public class PaymentsTableNode
{
    public required Category Category { get; init; }
    public Dictionary<string, PaymentsTableCell> Cells { get; init; } = new();
    public List<PaymentsTableNode> Children { get; init; } = new();
}

/// <summary>Ячейка таблицы: сумма и платежи категории вместе с её потомками за период.</summary>
public class PaymentsTableCell
{
    public decimal TotalSum { get; init; }
    public List<Payment> Payments { get; init; } = new();
}
