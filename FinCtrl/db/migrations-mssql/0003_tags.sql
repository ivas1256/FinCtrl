-- Добавление тегов для операций: таблица Tags и связка PaymentTags (many-to-many).
-- Без каскадного удаления (NO ACTION по умолчанию) — приложение само чистит
-- строки PaymentTags перед удалением тега.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE dbo.Tags
(
    TagId         INT             IDENTITY(1,1)   NOT NULL,
    TagName       NVARCHAR(255)                   NOT NULL,
    CreatedAt     DATETIME2                       NOT NULL,
    LastUpdatedAt DATETIME2                       NULL,
    CONSTRAINT PK_Tags PRIMARY KEY (TagId)
);
GO

CREATE UNIQUE INDEX UQ_Tags_TagName ON dbo.Tags (TagName);
GO

CREATE TABLE dbo.PaymentTags
(
    PaymentId INT NOT NULL,
    TagId     INT NOT NULL,
    CONSTRAINT PK_PaymentTags PRIMARY KEY (PaymentId, TagId),
    CONSTRAINT FK_PaymentTags_Payments FOREIGN KEY (PaymentId) REFERENCES dbo.Payments (PaymentId),
    CONSTRAINT FK_PaymentTags_Tags FOREIGN KEY (TagId) REFERENCES dbo.Tags (TagId)
);
GO

CREATE INDEX IX_PaymentTags_TagId ON dbo.PaymentTags (TagId);
GO
