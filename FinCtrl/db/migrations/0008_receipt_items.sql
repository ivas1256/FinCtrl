-- Позиции кассового чека, привязанные к платежу (задача 0041).
-- Units — справочник единиц измерения: название как напечатано в чеке («кг», «шт.»), уникальное.
-- Импорт будет искать единицу по названию и создавать недостающую (get-or-create, как PaymentSources).
-- ReceiptItems — одна строка на позицию чека: название, цена за единицу, количество, единица и итоговая цена.
-- У платежа может быть много позиций; заголовок чека (продавец, фискальные реквизиты) не хранится.
-- Каскадного удаления нет, как у PaymentTags и Transfers: платежи удаляются мягко (IsDeleted).
-- Файл в UTF-8 с BOM (кириллица в данных справочника). Идемпотентна.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Units', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Units
    (
        UnitId          INT             IDENTITY(1,1)   NOT NULL,
        UnitName        NVARCHAR(20)                    NOT NULL,
        CreatedAt       DATETIME2                       NOT NULL,
        LastUpdatedAt   DATETIME2                       NULL,
        CONSTRAINT PK_Units PRIMARY KEY (UnitId),
        CONSTRAINT UQ_Units_UnitName UNIQUE (UnitName)
    );
END
GO

-- Единицы, которые встречаются в чеках чаще всего; остальные добавит импорт.
INSERT INTO dbo.Units (UnitName, CreatedAt)
SELECT v.UnitName, SYSDATETIME()
FROM (VALUES (N'шт.'), (N'кг'), (N'г'), (N'л'), (N'мл')) AS v (UnitName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Units u WHERE u.UnitName = v.UnitName);
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
        UnitId          INT                             NOT NULL,
        TotalPrice      DECIMAL(18, 2)                  NOT NULL,
        CreatedAt       DATETIME2                       NOT NULL,
        LastUpdatedAt   DATETIME2                       NULL,
        CONSTRAINT PK_ReceiptItems PRIMARY KEY (ReceiptItemId),
        CONSTRAINT FK_ReceiptItems_Payments_PaymentId
            FOREIGN KEY (PaymentId) REFERENCES dbo.Payments (PaymentId),
        CONSTRAINT FK_ReceiptItems_Units_UnitId
            FOREIGN KEY (UnitId) REFERENCES dbo.Units (UnitId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReceiptItems_PaymentId' AND object_id = OBJECT_ID('dbo.ReceiptItems'))
    CREATE INDEX IX_ReceiptItems_PaymentId ON dbo.ReceiptItems (PaymentId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReceiptItems_UnitId' AND object_id = OBJECT_ID('dbo.ReceiptItems'))
    CREATE INDEX IX_ReceiptItems_UnitId ON dbo.ReceiptItems (UnitId);
GO
