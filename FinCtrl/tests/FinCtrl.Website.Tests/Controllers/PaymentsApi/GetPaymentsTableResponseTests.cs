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
    }
}
