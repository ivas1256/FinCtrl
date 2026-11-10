# План: [0041] Таблица позиций чеков, привязанных к платежам

## Затрагиваемые части
- БД: миграция `FinCtrl/db/migrations/0008_receipt_items.sql`
- Backend: код не меняется
- Frontend: нет

Контракт API: нет (задача только про схему).

## Шаги
- [x] 1. Миграция `0008_receipt_items.sql`: таблица `ReceiptItems`, FK `FK_ReceiptItems_Payments_PaymentId`, индекс `IX_ReceiptItems_PaymentId`
- [x] 2. Накатить на dev2 (`sqlcmd ... -I -b`), прогнать повторно
- [x] 3. Документация: `db/migrations/README.md`, `.ai-docs/project/project-summary.md`
- [x] Юнит-тесты: не нужны — SQL проверяется вручную на dev-базе (регламент, раздел 3)
- [x] Сборка + ручная проверка
- [ ] PR

## Ручная проверка
На dev2 (`FinCtrl_db_dev2`), 2026-11-10:
- два прогона миграции — оба без ошибок;
- `INFORMATION_SCHEMA.COLUMNS`: ReceiptItemId int, PaymentId int, ItemName nvarchar(500), Price decimal(18,2),
  Quantity decimal(18,3), Unit nvarchar(20), TotalPrice decimal(18,2), CreatedAt datetime2, LastUpdatedAt datetime2 NULL;
  остальные NOT NULL;
- есть `PK_ReceiptItems`, `IX_ReceiptItems_PaymentId`, `FK_ReceiptItems_Payments_PaymentId`;
- в транзакции с ROLLBACK вставлена позиция «Томаты 1кг», 199.99 × 0.624 кг = 124.79 — значения сохранились без округления;
- вставка с `PaymentId = -1` — ошибка 547 (FK); таблица после проверки пуста.

## Риски и открытые вопросы
- Сумма позиций чека может не совпадать с `PaymentSum`, если один платёж оплатил несколько чеков или чек оплачен
  частично — схема это не ограничивает, сверку оставляем коду импорта.

## Журнал решений
- 2026-11-10: одна таблица позиций без заголовка чека — постановка перечисляет только поля позиции.
- 2026-11-10: `LastUpdatedAt` вместо `UpdatedAt` — единое имя со всеми таблицами проекта.
- 2026-11-10: единица — строка как в чеке, без справочника: формат ОФД свободный («шт.», «кг»).

## Вне объёма (найдено попутно)
- Domain/репозиторий/API для позиций чека и импорт чеков из писем ОФД — отдельные задачи.

## Итог
Таблица `ReceiptItems` создана миграцией 0008 и накатана на dev2; код приложения не менялся.
