IF OBJECT_ID('tempdb..#month_totals') IS NOT NULL
  DROP TABLE #month_totals

SELECT
    p.PaymentDate
   ,p.PaymentSum
  ,p.PaymentDescription
--   ,FORMAT(SUM(p.PaymentSum), 'N2') sum_total
   ,COALESCE(c.CategoryName, c_ps.CategoryName) CategoryName
   ,COALESCE(c.OrderIndex, c_ps.OrderIndex) OrderIndex
  FROM Payments p
    JOIN PaymentSources ps ON ps.PaymentSourceId = p.PaymentSourceId
    LEFT JOIN Categories c_ps ON c_ps.CategoryId = ps.CategoryId
    LEFT JOIN Categories c ON c.CategoryId = p.PaymentCategoryCategoryId

  WHERE 1=1
    AND p.PaymentDate >= DATEFROMPARTS(2023, 01, 01)
    AND COALESCE(c.IsOnlyIncome, c_ps.IsOnlyIncome) = 0
  AND COALESCE(c.OrderIndex, c_ps.OrderIndex) IN (8,9)

SELECT
  * INTO #month_totals
FROM (SELECT
    DATEPART(MONTH, p.PaymentDate) PaymentDate
   ,SUM(p.PaymentSum) * -1 sum_total
--   ,FORMAT(SUM(p.PaymentSum), 'N2') sum_total
   ,COALESCE(c.CategoryName, c_ps.CategoryName) CategoryName
   ,COALESCE(c.OrderIndex, c_ps.OrderIndex) OrderIndex
  FROM Payments p
    JOIN PaymentSources ps ON ps.PaymentSourceId = p.PaymentSourceId
    LEFT JOIN Categories c_ps ON c_ps.CategoryId = ps.CategoryId
    LEFT JOIN Categories c ON c.CategoryId = p.PaymentCategoryCategoryId

  WHERE 1=1
    AND p.PaymentDate >= DATEFROMPARTS(2023, 01, 01)
    AND COALESCE(c.IsOnlyIncome, c_ps.IsOnlyIncome) = 0
  AND COALESCE(c.OrderIndex, c_ps.OrderIndex) IN (8,9)

  GROUP BY COALESCE(c.CategoryName, c_ps.CategoryName)
          ,DATEPART(MONTH, p.PaymentDate)
          ,COALESCE(c.OrderIndex, c_ps.OrderIndex)) t



SELECT
  *
  FROM #month_totals mt

--  RETURN

DECLARE @ColumnNames NVARCHAR(MAX);
SELECT @ColumnNames = STRING_AGG(QUOTENAME(PaymentDate), ',')
    FROM (SELECT DISTINCT PaymentDate FROM #month_totals) mt


DECLARE @query NVARCHAR(MAX)
SET @query = '           
              SELECT * FROM 
             (
                 SELECT * FROM #month_totals
             ) x
             PIVOT 
             (
                 max(sum_total)
                 FOR [PaymentDate] IN (' + @ColumnNames + ')
            ) p    

            '     
EXEC SP_EXECUTESQL @query

--
--SELECT
--  p.CategoryName
-- ,p.[1] 'Январь'
-- ,p.[2] 'Февраль'
-- ,p.[3] 'Март'
-- ,p.[4] 'Апрель'
-- ,p.[5] 'Май'
-- ,p.[6] 'Июнь'
-- ,p.[7] 'Июль'
-- ,p.[8] 'Август'
-- ,p.[9] 'Сентябрь'
-- ,p.[10] 'Октябрь'
-- ,p.[11] 'Ноябрь'
-- ,p.[12] 'Декабрь'
--FROM (SELECT
--    *
--  FROM #month_totals) t
--PIVOT (
--MAX(t.sum_total)
--FOR t.PaymentDate IN ([1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12])
--) p
--  ORDER BY p.OrderIndex
