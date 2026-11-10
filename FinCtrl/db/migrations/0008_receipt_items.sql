-- Позиции кассового чека, привязанные к платежу (задача 0041).
-- Одна строка — одна позиция чека: название, цена за единицу, количество с единицей измерения
-- («кг», «шт.», «г», «л» — как напечатано в чеке) и итоговая цена позиции.
-- У платежа может быть много позиций; заголовок чека (продавец, фискальные реквизиты) не хранится.
-- Каскадного удаления нет, как у PaymentTags и Transfers: платежи удаляются мягко (IsDeleted).

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.ReceiptItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceiptItems
    (
        ReceiptItemId   INT             IDENTITY(1,1)   NOT NULL,
        PaymentId       INT                             NOT NULL,
        ItemName        NVARCHAR(500)                   NOT NULL,
        Price           DECIMAL(18, 2)                  NOT NULL,
        Quantity        DECIMAL(18, 3)                  NOT NULL,
        Unit            NVARCHAR(20)                    NOT NULL,
        TotalPrice      DECIMAL(18, 2)                  NOT NULL,
        CreatedAt       DATETIME2                       NOT NULL,
        LastUpdatedAt   DATETIME2                       NULL,
        CONSTRAINT PK_ReceiptItems PRIMARY KEY (ReceiptItemId),
        CONSTRAINT FK_ReceiptItems_Payments_PaymentId
            FOREIGN KEY (PaymentId) REFERENCES dbo.Payments (PaymentId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReceiptItems_PaymentId' AND object_id = OBJECT_ID('dbo.ReceiptItems'))
    CREATE INDEX IX_ReceiptItems_PaymentId ON dbo.ReceiptItems (PaymentId);
GO
