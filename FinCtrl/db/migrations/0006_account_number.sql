-- Номер счёта (задача 0038).
-- Accounts.AccountNumber — номер счёта из колонки D xlsx-выгрузки банка; по нему импорт
-- сопоставляет счёт (get-or-create). NULL — номер неизвестен (счета, созданные до задачи,
-- получают номер при следующем импорте, если совпало название).
-- Ключом становится номер, поэтому уникальность ExcelAccountName снимается.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Accounts', 'AccountNumber') IS NULL
    ALTER TABLE dbo.Accounts ADD AccountNumber NVARCHAR(50) NULL;
GO

-- Фильтр: счетов без номера может быть много.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Accounts_AccountNumber' AND object_id = OBJECT_ID('dbo.Accounts'))
    CREATE UNIQUE INDEX UQ_Accounts_AccountNumber ON dbo.Accounts (AccountNumber)
        WHERE AccountNumber IS NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Accounts_ExcelAccountName' AND object_id = OBJECT_ID('dbo.Accounts'))
    DROP INDEX UQ_Accounts_ExcelAccountName ON dbo.Accounts;
GO
