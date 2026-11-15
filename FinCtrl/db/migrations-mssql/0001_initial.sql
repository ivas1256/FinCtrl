-- Baseline: структура БД FinCtrl_db_dev2 по состоянию на 2026-09-08.
-- Для существующей dev2-базы этот файл не выполнялся — она уже находится
-- в этом состоянии. Нужен для разворачивания БД с нуля и как точка отсчёта
-- для следующих миграций.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE dbo.Categories
(
    CategoryId                 INT             IDENTITY(1,1)   NOT NULL,
    CategoryName                NVARCHAR(MAX)   NULL,
    CreatedAt                   DATETIME2       NOT NULL,
    LastUpdatedAt                DATETIME2       NULL,
    ParentCategoryCategoryId    INT             NULL,
    IsOnlyIncome                 BIT             NOT NULL DEFAULT (CONVERT([bit], (0))),
    OrderIndex                   INT             NOT NULL DEFAULT ((0)),
    Color                        NVARCHAR(7)     NOT NULL DEFAULT ('#808080'),
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Categories_ParentCategoryCategoryId
        FOREIGN KEY (ParentCategoryCategoryId) REFERENCES dbo.Categories (CategoryId)
);
GO

CREATE INDEX IX_Categories_ParentCategoryCategoryId ON dbo.Categories (ParentCategoryCategoryId);
GO

CREATE TABLE dbo.PaymentSources
(
    PaymentSourceId     INT             IDENTITY(1,1)   NOT NULL,
    PaymentSourceName    NVARCHAR(MAX)   NULL,
    CategoryId           INT             NULL,
    CreatedAt            DATETIME2       NOT NULL,
    LastUpdatedAt         DATETIME2       NULL,
    Description           NVARCHAR(MAX)   NULL,
    CONSTRAINT PK_PaymentSources PRIMARY KEY (PaymentSourceId),
    CONSTRAINT FK_PaymentSources_Categories_CategoryId
        FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (CategoryId)
);
GO

CREATE INDEX IX_PaymentSources_CategoryId ON dbo.PaymentSources (CategoryId);
GO

CREATE TABLE dbo.Payments
(
    PaymentId                    INT             IDENTITY(1,1)   NOT NULL,
    PaymentType                   INT             NOT NULL,
    PaymentSum                    DECIMAL(18, 2)  NOT NULL,
    PaymentDate                   DATETIME2       NOT NULL,
    PaymentSourceId                INT             NULL,
    PaymentCategoryCategoryId      INT             NULL,
    PaymentDescription              NVARCHAR(MAX)   NULL,
    CreatedAt                     DATETIME2       NOT NULL,
    LastUpdatedAt                  DATETIME2       NULL,
    IsDeleted                      BIT             NOT NULL DEFAULT ((0)),
    ExcludeFromStats                BIT             NOT NULL DEFAULT ((0)),
    CONSTRAINT PK_Payments PRIMARY KEY (PaymentId),
    CONSTRAINT FK_Payments_Categories_PaymentCategoryCategoryId
        FOREIGN KEY (PaymentCategoryCategoryId) REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT FK_Payments_PaymentSources_PaymentSourceId
        FOREIGN KEY (PaymentSourceId) REFERENCES dbo.PaymentSources (PaymentSourceId)
);
GO

CREATE INDEX IX_Payments_Date_Deleted ON dbo.Payments (PaymentDate, IsDeleted);
CREATE INDEX IX_Payments_PaymentCategoryCategoryId ON dbo.Payments (PaymentCategoryCategoryId);
CREATE INDEX IX_Payments_PaymentSourceId ON dbo.Payments (PaymentSourceId);
CREATE INDEX IX_Payments_SourceId ON dbo.Payments (PaymentSourceId) WHERE IsDeleted = 0;
GO
