-- Накопления по накопительным счетам (задача 0040).
-- Строка «Накопления» на главной теперь считается по счетам с Accounts.IsSavings = 1, а не по категории,
-- поэтому снимаем собственную категорию, проставленную вручную ради этой строки:
--   1) категорию «Накопления» (id 33) у операций 2026 года;
--   2) категорию «Проценты на остаток» у операций со счётом.
-- Саму категорию, категории источников (PaymentSources.CategoryId) и операции без счёта не трогаем.
-- Только данные; повторный прогон безвреден (после первого строк под условие не остаётся).
-- Накатывать с -I.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

UPDATE dbo.Payments
SET PaymentCategoryCategoryId = NULL, LastUpdatedAt = SYSDATETIME()
WHERE PaymentCategoryCategoryId = 33
  AND PaymentDate >= '2026-01-01' AND PaymentDate < '2027-01-01';
GO

DECLARE @interestCategoryId INT = (SELECT TOP 1 CategoryId FROM dbo.Categories WHERE CategoryName = N'Проценты на остаток');

IF @interestCategoryId IS NOT NULL
    UPDATE dbo.Payments
    SET PaymentCategoryCategoryId = NULL, LastUpdatedAt = SYSDATETIME()
    WHERE PaymentCategoryCategoryId = @interestCategoryId
      AND AccountId IS NOT NULL;
GO
