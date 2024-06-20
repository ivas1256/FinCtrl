SELECT
  DATEPART(MONTH, p.PaymentDate) [month]
 ,MAX(COALESCE(p.PaymentCategoryCategoryId, ps.CategoryId)) CategoryId
 ,MAX(COALESCE(c1.CategoryName, c.CategoryName)) CategoryName
 ,SUM(p.PaymentSum) PaymentSum
FROM Payments p
  JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
  LEFT JOIN Categories c ON ps.CategoryId = c.CategoryId
  LEFT JOIN Categories c1 ON p.PaymentCategoryCategoryId = c1.CategoryId
WHERE p.PaymentDate >= DATEFROMPARTS(2023, 01, 01)
GROUP BY DATEPART(MONTH, p.PaymentDate)
        ,COALESCE(p.PaymentCategoryCategoryId, ps.CategoryId)
        ,COALESCE(c1.CategoryName, c.CategoryName)
        ,COALESCE(c1.OrderIndex, c.OrderIndex)
ORDER BY COALESCE(c1.OrderIndex, c.OrderIndex)

SELECT
  DATEPART(MONTH, p.PaymentDate) [month]
 ,DATEPART(WEEK, p.PaymentDate) [week]
  -- ,MAX(ps.PaymentSourceName) PaymentSourceName
  ,COALESCE(c1.CategoryName, c.CategoryName)
 ,SUM(p.PaymentSum) PaymentSum
 ,COUNT(p.PaymentSum) c
FROM Payments p
  JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
  LEFT JOIN Categories c ON ps.CategoryId = c.CategoryId
  LEFT JOIN Categories c1 ON p.PaymentCategoryCategoryId = c1.CategoryId
WHERE p.PaymentDate >= DATEFROMPARTS(2023, 01, 01)
--  AND COALESCE(p.PaymentCategoryCategoryId, ps.CategoryId) = 20
GROUP BY DATEPART(MONTH, p.PaymentDate)
        ,DATEPART(WEEK, p.PaymentDate)
         --        ,ps.PaymentSourceName
        ,COALESCE(c1.CategoryName, c.CategoryName)
        ,COALESCE(c1.OrderIndex, c.OrderIndex)
--ORDER BY ps.PaymentSourceName

SELECT
  DATEPART(MONTH, p.PaymentDate) [month]
 ,DATEPART(WEEK, p.PaymentDate) [week]
,COALESCE(c1.CategoryName, c.CategoryName)
 ,p.PaymentDate
 ,ps.PaymentSourceName
 ,p.PaymentSum
FROM Payments p
  JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
  LEFT JOIN Categories c ON ps.CategoryId = c.CategoryId
  LEFT JOIN Categories c1 ON p.PaymentCategoryCategoryId = c1.CategoryId
WHERE p.PaymentDate >= DATEFROMPARTS(2023, 01, 01)
--  AND COALESCE(p.PaymentCategoryCategoryId, ps.CategoryId) = 20


SELECT
  t.CategoryId
 ,t.CategoryName
 ,SUM(t.PaymentSum)
 ,AVG(t.PaymentSum)
 ,COUNT(t.PaymentSum)
FROM (SELECT
    DATEPART(MONTH, p.PaymentDate) [month]
   ,MAX(COALESCE(p.PaymentCategoryCategoryId, ps.CategoryId)) CategoryId
   ,MAX(COALESCE(c1.CategoryName, c.CategoryName)) CategoryName
   ,SUM(p.PaymentSum) PaymentSum
  FROM Payments p
  JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
  LEFT JOIN Categories c ON ps.CategoryId = c.CategoryId
  LEFT JOIN Categories c1 ON p.PaymentCategoryCategoryId = c1.CategoryId
  WHERE p.PaymentDate >= DATEFROMPARTS(2023, 01, 01)
  GROUP BY DATEPART(MONTH, p.PaymentDate)
          ,COALESCE(p.PaymentCategoryCategoryId, ps.CategoryId)
          ,COALESCE(c1.CategoryName, c.CategoryName)
          ,COALESCE(c1.OrderIndex, c.OrderIndex)
--ORDER BY COALESCE(c1.OrderIndex, c.OrderIndex)
) t

GROUP BY t.CategoryId
        ,t.CategoryName