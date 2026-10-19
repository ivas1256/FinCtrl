-- Добавление таблицы Accounts (счета) и связи с Payments.
-- Счёт определяется двумя полями: ExcelAccountName (как счёт называется
-- в исходном xlsx-файле банка, используется для get-or-create при импорте)
-- и DisplayName (название, которое показывается в интерфейсе).

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE dbo.Accounts
(
    AccountId           INT             IDENTITY(1,1)   NOT NULL,
    ExcelAccountName      NVARCHAR(255)   NOT NULL,
    DisplayName            NVARCHAR(255)   NOT NULL,
    CreatedAt              DATETIME2       NOT NULL,
    LastUpdatedAt            DATETIME2       NULL,
    CONSTRAINT PK_Accounts PRIMARY KEY (AccountId)
);
GO

CREATE UNIQUE INDEX UQ_Accounts_ExcelAccountName ON dbo.Accounts (ExcelAccountName);
GO

ALTER TABLE dbo.Payments ADD AccountId INT NULL;
GO

ALTER TABLE dbo.Payments ADD CONSTRAINT FK_Payments_Accounts_AccountId
    FOREIGN KEY (AccountId) REFERENCES dbo.Accounts (AccountId);
GO

CREATE INDEX IX_Payments_AccountId ON dbo.Payments (AccountId);
GO
