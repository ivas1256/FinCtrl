SELECT
  p.PaymentId
 ,p.PaymentSum
 ,p.PaymentDate
 ,p.PaymentDescription
 ,ps.PaymentSourceId
 ,ps.PaymentSourceName
 ,COALESCE(pc.CategoryId, c.CategoryId, -1) CategoryId
 ,COALESCE(pc.CategoryName, c.CategoryName, 'Без категории') CategoryName
FROM Payments p
  JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
  LEFT JOIN Categories pc ON p.PaymentCategoryCategoryId = pc.CategoryId
  LEFT JOIN Categories c ON ps.CategoryId = c.CategoryId
WHERE DATEPART(YEAR, p.PaymentDate) = 2023


SELECT
  DATEPART(WEEK, p.PaymentDate)
 ,COALESCE(pc.CategoryId, c.CategoryId, -1) CategoryId
 ,COALESCE(pc.CategoryName, c.CategoryName, 'Без категории') CategoryName
 ,SUM(p.PaymentSum) PaymentSum
FROM Payments p
  JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
  LEFT JOIN Categories pc ON p.PaymentCategoryCategoryId = pc.CategoryId
  LEFT JOIN Categories c ON ps.CategoryId = c.CategoryId
WHERE DATEPART(YEAR, p.PaymentDate) = 2023
GROUP BY DATEPART(WEEK, p.PaymentDate)
        ,COALESCE(pc.CategoryId, c.CategoryId, -1)
        ,COALESCE(pc.CategoryName, c.CategoryName, 'Без категории')


SELECT
--  t.week_num
 t.CategoryId
 ,t.CategoryName
 ,AVG(t.PaymentSum) avg_sum
FROM (SELECT
    DATEPART(WEEK, p.PaymentDate) week_num
   ,COALESCE(pc.CategoryId, c.CategoryId, -1) CategoryId
   ,COALESCE(pc.CategoryName, c.CategoryName, 'Без категории') CategoryName
   ,SUM(p.PaymentSum) PaymentSum
  FROM Payments p
    JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
      LEFT JOIN Categories pc ON p.PaymentCategoryCategoryId = pc.CategoryId
        LEFT JOIN Categories c ON ps.CategoryId = c.CategoryId
  WHERE DATEPART(YEAR, p.PaymentDate) = 2023
  GROUP BY DATEPART(WEEK, p.PaymentDate)
          ,COALESCE(pc.CategoryId, c.CategoryId, -1)
          ,COALESCE(pc.CategoryName, c.CategoryName, 'Без категории')) t

  GROUP BY
  t.CategoryId
 ,t.CategoryName