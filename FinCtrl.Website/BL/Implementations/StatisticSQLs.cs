namespace FinCtrl.Website.BL.Implementations
{
    public class StatisticSQLs
    {
        public const string MAIN_STATISTIC = @"
SELECT
  p.PaymentId
 ,p.PaymentType
 ,p.PaymentSum
 ,p.PaymentDate
 ,p.PaymentDescription 
 ,p.PaymentCategoryCategoryId
 
,ps.PaymentSourceId
 ,ps.PaymentSourceName
,ps.Description

 ,COALESCE(pc.CategoryId, c.CategoryId, -1) CategoryId
 ,COALESCE(pc.CategoryName, c.CategoryName, 'Без категории') CategoryName
 ,COALESCE(pc.OrderIndex, c.OrderIndex, 99) OrderIndex
 ,COALESCE(pc.IsOnlyIncome, c.IsOnlyIncome, 0) IsOnlyIncome
FROM Payments p
  JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
  LEFT JOIN Categories pc ON p.PaymentCategoryCategoryId = pc.CategoryId
  LEFT JOIN Categories c ON ps.CategoryId = c.CategoryId

WHERE p.PaymentDate >= @from
AND p.PaymentDate < @to";
    }
}
