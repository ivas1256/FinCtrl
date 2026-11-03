-- Классификация источника платежа: человекочитаемая группа контрагента
-- (задаётся LLM-сервисом FinCtrl.AiAssistant при импорте или вручную).
-- IsAiClassified = 1 — значение проставила LLM; ручная правка сбрасывает флаг.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.PaymentSources', 'Classification') IS NULL
    ALTER TABLE dbo.PaymentSources ADD Classification NVARCHAR(200) NULL;
GO

IF COL_LENGTH('dbo.PaymentSources', 'IsAiClassified') IS NULL
    ALTER TABLE dbo.PaymentSources ADD IsAiClassified BIT NOT NULL
        CONSTRAINT DF_PaymentSources_IsAiClassified DEFAULT 0;
GO
