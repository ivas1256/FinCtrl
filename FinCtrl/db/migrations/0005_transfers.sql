-- Переводы между своими счетами (задача 0037).
-- Accounts.IsSavings — признак накопительного счёта: модалка правки платежа подставляет
-- категорию «Накопления», если вторая сторона перевода на таком счёте.
-- Transfers связывает две стороны перевода: списание (OutPaymentId, −) и поступление
-- (InPaymentId, +; NULL — вторая сторона не указана). Сами операции имеют PaymentType = 2 (Transfer).
-- Каскадного удаления нет, как у PaymentTags.
-- UQ_Payments_Account_Date_Sum — защита от дублей «счёт + дата + сумма».

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Accounts', 'IsSavings') IS NULL
    ALTER TABLE dbo.Accounts ADD IsSavings BIT NOT NULL
        CONSTRAINT DF_Accounts_IsSavings DEFAULT 0;
GO

IF OBJECT_ID('dbo.Transfers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transfers
    (
        TransferId      INT         IDENTITY(1,1)   NOT NULL,
        OutPaymentId    INT                         NOT NULL,
        InPaymentId     INT                         NULL,
        CreatedAt       DATETIME2                   NOT NULL,
        CONSTRAINT PK_Transfers PRIMARY KEY (TransferId),
        CONSTRAINT FK_Transfers_Payments_OutPaymentId
            FOREIGN KEY (OutPaymentId) REFERENCES dbo.Payments (PaymentId),
        CONSTRAINT FK_Transfers_Payments_InPaymentId
            FOREIGN KEY (InPaymentId) REFERENCES dbo.Payments (PaymentId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Transfers_OutPaymentId' AND object_id = OBJECT_ID('dbo.Transfers'))
    CREATE UNIQUE INDEX UQ_Transfers_OutPaymentId ON dbo.Transfers (OutPaymentId);
GO

-- Фильтр: обычный UNIQUE допускает только один NULL, а переводов без второй стороны может быть много.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Transfers_InPaymentId' AND object_id = OBJECT_ID('dbo.Transfers'))
    CREATE UNIQUE INDEX UQ_Transfers_InPaymentId ON dbo.Transfers (InPaymentId)
        WHERE InPaymentId IS NOT NULL;
GO

-- Фильтр по AccountId: без него SQL Server считает все NULL равными, и операции без счёта
-- с одинаковыми датой и суммой не пройдут индекс. Фильтр по IsDeleted: мягко удалённая
-- операция не должна блокировать повторный импорт той же строки.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Payments_Account_Date_Sum' AND object_id = OBJECT_ID('dbo.Payments'))
BEGIN
    IF EXISTS (
        SELECT 1
        FROM dbo.Payments
        WHERE AccountId IS NOT NULL AND IsDeleted = 0
        GROUP BY AccountId, PaymentDate, PaymentSum
        HAVING COUNT(*) > 1)
        THROW 50001, N'В Payments есть дубли (AccountId, PaymentDate, PaymentSum) среди неудалённых операций — устраните их перед созданием UQ_Payments_Account_Date_Sum.', 1;

    CREATE UNIQUE INDEX UQ_Payments_Account_Date_Sum
        ON dbo.Payments (AccountId, PaymentDate, PaymentSum)
        WHERE AccountId IS NOT NULL AND IsDeleted = 0;
END
GO
