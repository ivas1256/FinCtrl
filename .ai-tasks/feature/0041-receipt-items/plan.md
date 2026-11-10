# План: [0041] Таблица позиций чеков, привязанных к платежам

## Затрагиваемые части
- БД: миграция `FinCtrl/db/migrations/0008_receipt_items.sql`
- Backend: Domain `Receipts/`, Application `Receipts/` (репозиторий-интерфейс + `ReceiptService`),
  Infrastructure `ReceiptItemRepository`, `PaymentsApiController.GetReceipt` + DTO
- Frontend: `src/pages/paymentsTable/ReceiptModal.vue` (новый) + spec, `PaymentList.vue` (кнопка, emit `open-receipt`),
  `PaymentsTable.vue` (подключение модалки)

## Контракт API
`GET api/PaymentsApi/{paymentId:int}/receipt`
- 200: `{ paymentId, totalSum, items: [ { receiptItemId, itemName, price, quantity, unitId, unitName, totalPrice } ] }`;
  `items` — по `ReceiptItemId`, `totalSum` — сумма `totalPrice` (0 при пустом чеке).
- Позиций нет → 200 с пустым `items`.
- Платёж во всех ответах (`PaymentDto` Website) — новое поле `hasReceipt: bool` (есть позиции чека); только чтение.
- Платёж не существует или удалён → 404 ProblemDetails.

## Шаги
- [x] 1. Миграция `0008_receipt_items.sql`: таблица `ReceiptItems`, FK `FK_ReceiptItems_Payments_PaymentId`, индекс `IX_ReceiptItems_PaymentId`
- [x] 1a. Справочник `Units` (`UQ_Units_UnitName`, заполнение шт./кг/г/л/мл), `ReceiptItems.UnitId` FK + индекс `IX_ReceiptItems_UnitId`; файл в UTF-8 с BOM
- [x] 2. Накатить на dev2 (`sqlcmd ... -I -b`), прогнать повторно
- [x] 3. Документация: `db/migrations/README.md`, `.ai-docs/project/project-summary.md`
- [x] Юнит-тесты: не нужны — SQL проверяется вручную на dev-базе (регламент, раздел 3)
- [x] Сборка + ручная проверка
- [x] 4. Domain: `Unit`, `ReceiptItem`, read-модель `Receipt`
- [x] 5. Application: `IReceiptItemRepository`, `ReceiptService` (+ DI), юнит-тесты
- [x] 6. Infrastructure: `ReceiptItemRepository` (Dapper, JOIN Units) + DI
- [x] 7. Website: `GetReceipt` в `PaymentsApiController`, DTO `ReceiptDto`/`ReceiptItemDto`
- [x] 8. Сборка/тесты, ручная проверка API на dev2
- [x] 9. Frontend: `ReceiptModal.vue` — вид «бумажный чек», загрузка/пусто/ошибка; Vitest-тесты; `formatQuantity` в `utils/format.js`
- [x] 10. Frontend: кнопка 🧾 в `PaymentList.vue` (emit `open-receipt`), модалка в `PaymentsTable.vue`
- [x] 11. `check.mjs frontend`, ручная проверка в preview (8270 — 12 позиций, 8269 — пусто)
- [x] 12. Backend: `Payment.HasReceipt` — `SelectPayment` (`EXISTS ReceiptItems`), `PaymentDto` Infrastructure + `MapToPayment`, `PaymentDto` Website + `FromModel`; тесты
- [x] 13. Frontend: кнопка 🧾 в `PaymentList.vue` `disabled` при `!payment.hasReceipt`, title «Чека нет»; тесты
- [x] 14. `check.mjs all`, ручная проверка (8270 — активна, 8269 — неактивна)
- [ ] PR (не делается: пользователь просил без коммита)

## Ручная проверка
На dev2 (`FinCtrl_db_dev2`), 2026-11-10:
- два прогона миграции — оба без ошибок;
- `INFORMATION_SCHEMA.COLUMNS`: ReceiptItemId int, PaymentId int, ItemName nvarchar(500), Price decimal(18,2),
  Quantity decimal(18,3), UnitId int, TotalPrice decimal(18,2), CreatedAt datetime2, LastUpdatedAt datetime2 NULL;
  остальные NOT NULL;
- есть `PK_Units`, `UQ_Units_UnitName`, `PK_ReceiptItems`, `IX_ReceiptItems_PaymentId`, `IX_ReceiptItems_UnitId`,
  `FK_ReceiptItems_Payments_PaymentId`, `FK_ReceiptItems_Units_UnitId`;
- `Units` — 5 строк (шт., кг, г, л, мл), кириллица записана верно (UNICODE первого символа 1096/1082/1075/1083/1084);
  второй прогон миграции дублей не добавил (0 rows);
- в транзакции с ROLLBACK вставлена позиция «Томаты 1кг», 199.99 × 0.624 кг = 124.79 с единицей, найденной по `N'кг'`, —
  значения сохранились без округления;
- вставка с `PaymentId = -1` и с `UnitId = -1` — ошибка 547 (FK); повторная единица «кг» — 2627 (уникальность);
  `ReceiptItems` после проверки пуста.

Ручная проверка API (preview `backend`, dev2, 2026-11-10):
- `GET /api/PaymentsApi/8270/receipt` — 200, 12 позиций по `receiptItemId` 3…14, первая «Томаты 1кг» 199.99 × 0.624 кг = 124.79,
  `totalSum` 1365.89 (совпадает с `SUM(TotalPrice)` в БД);
- `8269` (без позиций) — 200, `items: []`, `totalSum: 0`;
- `-1` — 404 «Операция -1 не найдена»; мягко удалённый платёж 3312 — 404; ошибок в логах backend нет.
- `check.mjs backend`: build OK, tests 441/441 OK.

Ручная проверка UI (preview `backend` + `frontend`, dev2, 2026-11-10):
- главная → ячейка «Еда · ноя 2026» → у платежа 8270 кнопки «Открыть чек» и «Редактировать платеж»;
- чек 8270: `modal-dialog` шириной 500px, моноширинный шрифт тела, 12 позиций («Томаты 1кг / 0,624 кг × 199,99 ₽ / 124,79 ₽»),
  «ИТОГ 1 365,89 ₽», строки «Сумма операции» нет (суммы совпадают); полей ввода 0, кнопки — только «Закрыть»;
- чек 8269: «У этой операции нет чека»;
- ошибок в консоли (кроме 405 от собственного пробного запроса) и в логах backend нет; 404 покрыт Vitest-тестом.
- `check.mjs frontend`: build OK, tests 304/304 OK.

Ручная проверка `hasReceipt` (2026-11-10):
- SQL на dev2: выражение из `SelectPayment` даёт 1 для 8270 и 0 для 8269;
- главная: у 8270 («Еда · ноя 2026») кнопка активна, title «Открыть чек», модалка открывается («ИТОГ 1 365,89 ₽»);
  у 8269 («Без категории») кнопка `disabled`, title «Чека нет»; ошибок в логах backend нет;
- `check.mjs all`: backend 441/441, frontend 306/306.
- Тест `PaymentRepositoryTests.GetSelectColumnNames` теперь режет список колонок по `FROM Payments` (подзапрос `EXISTS … FROM ReceiptItems`); в подзапросах колонок `SelectPayment` нельзя использовать запятые.

## Риски и открытые вопросы
- Сумма позиций чека может не совпадать с `PaymentSum`, если один платёж оплатил несколько чеков или чек оплачен
  частично — схема это не ограничивает, сверку оставляем коду импорта.

## Журнал решений
- 2026-11-10: одна таблица позиций без заголовка чека — постановка перечисляет только поля позиции.
- 2026-11-10: `LastUpdatedAt` вместо `UpdatedAt` — единое имя со всеми таблицами проекта.
- 2026-11-10: единица сначала была строкой в позиции; по запросу пользователя вынесена в справочник `Units`. Миграция 0008 ещё не смёржена — исправлена на месте, пустая `ReceiptItems` на dev2 пересоздана.

- 2026-11-10: по запросу пользователя в задачу добавлено API чтения чека; работа в той же ветке без коммита.
- 2026-11-10: платёж без позиций — 200 с пустым списком, а не 404.
- 2026-11-10: UI чека — из трёх макетов выбран «бумажный чек»; кнопка — на главной в `PaymentList` (выбор пользователя).

## Вне объёма (найдено попутно)
- Запись/правка позиций через API и импорт чеков из писем ОФД — отдельные задачи.
- `hasReceipt` нет в `TopPaymentDto` дашборда (отдельная урезанная модель) — не нужно, пока там нет кнопки чека.

## Итог
Таблицы `Units` и `ReceiptItems` созданы миграцией 0008 и накатаны на dev2. Добавлено API чтения чека `GET api/PaymentsApi/{paymentId}/receipt` с юнит-тестами `ReceiptService`. На главной добавлен просмотр чека: кнопка 🧾 в `PaymentList` и модалка `ReceiptModal` в виде бумажного чека. Изменения API и UI не закоммичены (по запросу пользователя).
