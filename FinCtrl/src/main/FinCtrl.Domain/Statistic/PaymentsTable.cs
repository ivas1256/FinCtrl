using FinCtrl.Common;

namespace FinCtrl.Domain.Statistic;

/// <summary>Read-модель таблицы главной страницы: дерево категорий × периоды группировки.</summary>
public class PaymentsTable
{
    /// <summary>Периоды по возрастанию.</summary>
    public List<PaymentsTablePeriod> Periods { get; init; } = new();

    /// <summary>Корневые категории, затем «Без категории» и итоговая строка.</summary>
    public List<PaymentsTableNode> Tree { get; init; } = new();

    /// <summary>Периоды сравнения с прошлым периодом; суммы — в <see cref="PaymentsTableNode.Comparison"/>.</summary>
    public PaymentsTableComparison? Comparison { get; init; }
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

    /// <summary>Суммы строки за периоды <see cref="PaymentsTable.Comparison"/>.</summary>
    public PaymentsTableNodeComparison? Comparison { get; init; }
}

/// <summary>Ячейка таблицы: сумма и платежи категории вместе с её потомками за период.</summary>
public class PaymentsTableCell
{
    public decimal TotalSum { get; init; }
    public List<Payment> Payments { get; init; } = new();
}

/// <summary>
/// Пара периодов для сравнения: текущий период группировки по дату <see cref="AsOf"/> включительно
/// и прошлый период целиком.
/// </summary>
public class PaymentsTableComparison
{
    /// <summary>Дата окончания сравнения D = min(сегодня, последний день фильтра).</summary>
    public required DateTime AsOf { get; init; }

    /// <summary>Текущий период: ключ периода группировки и диапазон <c>[начало периода, D + 1 день)</c>.</summary>
    public required PaymentsTablePeriod Current { get; init; }

    /// <summary>Прошлый период целиком.</summary>
    public required PaymentsTablePeriod Previous { get; init; }

    /// <summary>Конец полного текущего периода (исключающая граница).</summary>
    public required DateTime CurrentPeriodTo { get; init; }

    /// <summary>Текущий период ещё не закончился к дате D.</summary>
    public bool IsCurrentPartial => Current.Range.To < CurrentPeriodTo;
}

/// <summary>Суммы строки за текущий и прошлый периоды сравнения (вместе с потомками).</summary>
public class PaymentsTableNodeComparison
{
    public decimal Current { get; init; }
    public decimal Previous { get; init; }
}
