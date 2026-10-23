using FinCtrl.Common;
using FinCtrl.Domain;
using FinCtrl.Domain.Statistic;
using FinCtrl.Website.Controllers.PaymentsApi;

namespace FinCtrl.Website.Tests.Controllers.PaymentsApi;

public class GetPaymentsTableResponseTests
{
    private static readonly DateRange January = new(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

    [Fact]
    public void FromModel_Table_MapsPeriodsTreeAndCells()
    {
        var food = new Category(1) { CategoryName = "Еда", Color = "" };
        var cafe = new Category(2) { CategoryName = "Кафе", ParentCategoryId = 1, Color = "#112233" };
        var payment = new Payment { PaymentId = 10, PaymentSum = -50, PaymentCategory = cafe };
        var table = new PaymentsTable
        {
            Periods = { new PaymentsTablePeriod { Key = "012026", Range = January } },
            Tree =
            {
                new PaymentsTableNode
                {
                    Category = food,
                    Cells = { ["012026"] = new PaymentsTableCell { TotalSum = -50, Payments = { payment } } },
                    Children =
                    {
                        new PaymentsTableNode
                        {
                            Category = cafe,
                            Cells = { ["012026"] = new PaymentsTableCell { TotalSum = -50, Payments = { payment } } }
                        }
                    }
                }
            }
        };

        var response = GetPaymentsTableResponse.FromModel(table);

        var period = Assert.Single(response.Periods);
        Assert.Equal("012026", period.Key);
        Assert.Equal(new DateRangeDto(January.From, January.To), period.Value);

        var root = Assert.Single(response.PaymentsTree);
        Assert.Equal("Еда", root.CategoryName);
        Assert.Equal(Category.DefaultColor, root.Color);
        Assert.Equal(-50, root.Periods["012026"].TotalSum);
        Assert.Equal(10, Assert.Single(root.Periods["012026"].Payments).PaymentId);

        var child = Assert.Single(root.Children);
        Assert.Equal(1, child.ParentCategoryId);
        Assert.Equal("#112233", child.Color);
        Assert.Equal(2, Assert.Single(child.Periods["012026"].Payments).Category!.CategoryId);
        Assert.Null(response.Comparison);
        Assert.Null(root.Comparison);
    }

    [Fact]
    public void FromModel_Comparison_MapsPeriodsAndNodeSums()
    {
        var table = new PaymentsTable
        {
            Tree =
            {
                new PaymentsTableNode
                {
                    Category = new Category(1),
                    Comparison = new PaymentsTableNodeComparison { Current = -24650, Previous = -31200 },
                    Children =
                    {
                        new PaymentsTableNode
                        {
                            Category = new Category(2),
                            Comparison = new PaymentsTableNodeComparison { Current = -100, Previous = 0 }
                        }
                    }
                }
            },
            Comparison = new PaymentsTableComparison
            {
                AsOf = new DateTime(2026, 10, 21),
                Current = new PaymentsTablePeriod { Key = "102026", Range = new DateRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 22)) },
                Previous = new PaymentsTablePeriod { Key = "092026", Range = new DateRange(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1)) },
                CurrentPeriodTo = new DateTime(2026, 11, 1)
            }
        };

        var response = GetPaymentsTableResponse.FromModel(table);

        var comparison = response.Comparison!;
        Assert.Equal(new DateTime(2026, 10, 21), comparison.AsOf);
        Assert.Equal(new ComparisonPeriodDto("102026", new DateTime(2026, 10, 1), new DateTime(2026, 10, 22)), comparison.Current);
        Assert.Equal(new ComparisonPeriodDto("092026", new DateTime(2026, 9, 1), new DateTime(2026, 10, 1)), comparison.Previous);
        Assert.Equal(new DateTime(2026, 11, 1), comparison.CurrentPeriodTo);
        Assert.True(comparison.IsCurrentPartial);

        var root = Assert.Single(response.PaymentsTree);
        Assert.Equal(new NodeComparisonDto(-24650, -31200), root.Comparison);
        Assert.Equal(new NodeComparisonDto(-100, 0), Assert.Single(root.Children).Comparison);
    }
}
