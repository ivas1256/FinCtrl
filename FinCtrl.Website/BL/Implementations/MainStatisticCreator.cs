using Dapper;
using FinCtrl.Domain;
using FinCtrl.Website.BL.Data;
using FinCtrl.Website.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinCtrl.Website.BL.Implementations
{
    public class MainStatisticCreator
    {
        //readonly DapperDBContext _dapper;
        readonly Category _nullCategory = new Category(-99999, "Без категории");

        //public MainStatisticCreator(DapperDBContext dapper)
        //{
        //    _dapper = dapper;
        //}

        public List<MainStatisticGroupItem> Calc(DatePeriod period, GroupingType? groupingType)
        {
            var payments = QueryPaymentsList(period);

            var fakePaymentsSummary = payments
                .GroupBy(x => GetGropingDate(x.PaymentDate, groupingType))
                .Select(x => new MainStatisticGroupItem()
                {
                    Period = new DatePeriod(x.Key.Date, GetPeriodEndDate(x.Key.Date, groupingType)),
                    Category = new Category()
                    {
                        CategoryId = 999999999,
                        CategoryName = "ВСЕГО потрачено:",
                        OrderIndex = 999999999
                    },
                    TotalSum = x.Where(x => x.Category.IsIncludeInTotalResults())
                                .Sum(y => y.PaymentSum),
                    Payments = x.Where(x => x.Category.IsIncludeInTotalResults())
                                .OrderByDescending(x => x.PaymentDate)
                                .ToList()
                });

            return payments
                .GroupBy(x => new
                {
                    Date = GetGropingDate(x.PaymentDate, groupingType),
                    Category = x.Category ?? _nullCategory,
                })
                .Select(x => new MainStatisticGroupItem()
                {
                    Period = new DatePeriod(x.Key.Date, GetPeriodEndDate(x.Key.Date, groupingType)),
                    Category = x.Key.Category,
                    TotalSum = x.Sum(y => y.PaymentSum),
                    Payments = x.OrderByDescending(x => x.PaymentDate).ToList()
                })
                .Union(fakePaymentsSummary)
                .OrderBy(x => x.Category?.OrderIndex ?? _nullCategory.ID)
                .ToList();
        }

        DateTime GetGropingDate(DateTime paymentDate, GroupingType? groupingType)
        {
            switch (groupingType)
            {
                case Controllers.GroupingType.Month:
                    return new DateTime(paymentDate.Year, paymentDate.Month, 1);
                case Controllers.GroupingType.Week:
                    return paymentDate.AddDays((int)paymentDate.DayOfWeek * -1).Date;
                case Controllers.GroupingType.Day:
                    return paymentDate.Date;
                default:
                    throw new NotImplementedException(groupingType.ToString());
            }
        }

        DateTime GetPeriodEndDate(DateTime periodStartDate, GroupingType? groupingType)
        {
            switch (groupingType)
            {
                case Controllers.GroupingType.Month:
                    return periodStartDate.AddMonths(1);
                case Controllers.GroupingType.Week:
                    return periodStartDate.AddDays(7).Date;
                case Controllers.GroupingType.Day:
                    return periodStartDate.Date;
                default:
                    throw new NotImplementedException(groupingType.ToString());
            }
        }

        IEnumerable<GeneralStatisticListItem> QueryPaymentsList(DatePeriod period)
        {
            using (var conn = _dapper.SqlConnection)
                return conn.Query<GeneralStatisticListItem, PaymentSource, Category, GeneralStatisticListItem>
                    (StatisticSQLs.MAIN_STATISTIC, (item, source, category) =>
                        {
                            item.Category = category;
                            item.PaymentSource = source;
                            return item;
                        },
                    splitOn: "PaymentSourceId,CategoryId",
                    param: new { from = period.From, to = period.To })
                    .OrderBy(x => x.PaymentDate);
        }
    }
}
