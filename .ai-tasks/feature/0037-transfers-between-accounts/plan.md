# План: [0037] Переводы между своими счетами

## Затрагиваемые части
- БД: миграция `0005_transfers.sql`
- Backend: Domain (`PaymentTypeEnum`, `Transfer`, `Account`, `Payment`), Application (`Transfers/ITransferRepository`, `PaymentService`, импорт, статистика, дашборд), Infrastructure (`TransferRepository`, `PaymentRepository`, `AccountRepository`), Website (`PaymentDto`, `AccountDto`, `AccountsApiController`, `PaymentsApiController`)
- Frontend: `Accounts.vue`, `PaymentsTable.vue`, чистая функция автоподстановки категории

## Шаги
Этап 1 — схема БД:
- [x] Проверка дублей «счёт + дата + сумма» на dev-базе
- [x] Миграция `0005_transfers.sql` (IsSavings, Transfers, UQ_Payments_Account_Date_Sum), накатить, проверить идемпотентность и индекс
- [x] Domain: `PaymentTypeEnum.Transfer = 2`, `Transfer`, `Account.IsSavings`, `Payment.TransferId`/`TransferPairPaymentId`
- [x] `ITransferRepository` + `TransferRepository` + DI
- [x] `SelectPayment` + Infrastructure `PaymentDto` (+ `MapToPayment`) + Website `PaymentDto.FromModel`; `Account_IsSavings`
- [x] `AccountRepository`, `AccountDto`, `AccountsApiController` — `IsSavings`
- [x] `Accounts.vue` — флажок «Накопительный»
- [x] Тесты этапа 1, сборка, ручная проверка

Этап 2 — импорт:
- [x] `IPaymentRepository.FindDuplicate` → `GetByDateAndSum` (все кандидаты по дате и сумме, по возрастанию id)
- [x] `PaymentService.FindDuplicate(sum, date, accountId)`: тот же счёт → без счёта → `null`; без счёта у строки — первый кандидат
- [x] `PaymentsImportService` передаёт `account?.AccountId`
- [x] Тесты `PaymentServiceTests.FindDuplicate_*`, `PaymentsImportServiceTests` (перевод не сбрасывается, повтор в файле)
- [x] Сборка, тесты, проверка запроса на dev-базе

Этап 3 — модалка: план согласуется после этапа 2.
Этап 4 — аналитика: план согласуется после этапа 3.

- [ ] Документация `project-summary.md`
- [ ] PR

## Тесты
| Что тестируем | Сценарии |
|---------------|----------|
| `PaymentRepositoryTests` | алиасы `SelectPayment` ↔ `PaymentDto` с новыми колонками |
| Infrastructure `PaymentDtoTests` | маппинг `TransferId`, `TransferPairPaymentId`, `Account_IsSavings` |
| Website `PaymentDtoTests` | `FromModel` переносит поля перевода и `IsSavings` счёта |
| Domain `PaymentTests` | `NormalizeSumSign` для `Transfer` не меняет сумму |
| `PaymentServiceTests.FindDuplicate_*` | другой счёт — не дубль; тот же счёт; приоритет того же счёта над платежом без счёта; только без счёта — для дозаполнения; строка без счёта — прежнее поведение; нет кандидатов |
| `PaymentsImportServiceTests` | дубль на том же счёте не трогается; нет дубля на счёте строки — создаётся; `Transfer` без счёта — счёт дозаполнен, тип сохранён; `Transfer` на том же счёте — пропуск без `Update`; повтор строки в файле — вторая пропущена |

## Ручная проверка
- Миграция: повторный запуск без ошибок; INSERT дубля в транзакции падает на `UQ_Payments_Account_Date_Sum` (ROLLBACK).
- `TransferRepository` и join в `SelectPayment` — запросы на dev-базе.
- Preview: «Счета» — флажок сохраняется после перезагрузки; главная и «Операции» без ошибок, в ответе API есть `transferId`/`transferPairPaymentId`.

## Риски и открытые вопросы
- Если в базе уже есть дубли «счёт + дата + сумма», индекс не создастся — нужно решение пользователя.

## Журнал решений
- 2026-11-06: работа поэтапно в одной ветке, план каждого этапа согласуется; коммиты делает пользователь.
- 2026-11-06: повтор строки в файле тихо схлопывается `FindDuplicate`, ошибки нет.
- 2026-11-06: `Transfers.InPaymentId` — фильтрованный уникальный индекс (`WHERE InPaymentId IS NOT NULL`).
- 2026-11-06: фильтрованные индексы требуют `QUOTED_IDENTIFIER ON` для любого DML по `Payments`/`Transfers`; `sqlcmd` по умолчанию его выключает — в CLAUDE.md команда накатки миграций дополнена флагом `-I`. SqlClient включает опцию сам.
- 2026-11-06: этап 1 проверен: дублей «счёт + дата + сумма» в dev-базе нет; миграция накатана дважды без ошибок; дубль по счёту отклоняется индексом, две строки без счёта проходят (INSERT в транзакции с ROLLBACK); временная строка в `Transfers` (удалена после проверки) дала в `GET api/PaymentsApi/list` `transferId` и `transferPairPaymentId` второй стороны; «Счета» — флажок сохраняется и переживает перезагрузку, возвращён в исходное значение.
- 2026-11-06: этап 2 — при строке со счётом дубль ищется на том же счёте, затем среди платежей без счёта (им дозаполняется счёт). Только точный ключ «счёт + дата + сумма» продублировал бы при повторной загрузке тысячи старых платежей без счёта. Совпадение на том же счёте проверяется первым, поэтому дозаполнение не нарушает `UQ_Payments_Account_Date_Sum`. Выбор вынесен из SQL в `PaymentService` ради юнит-тестов.
- 2026-11-06: `PaymentType` найденного дубля импорт не меняет и раньше: `Update` пишет тип, прочитанный из БД. Сумма сравнивается как хранится — импорт пишет её как в файле, `NormalizeSumSign` работает только при чтении.
- 2026-11-06: этап 2 проверен: запрос `GetByDateAndSum` на dev-базе возвращает всех кандидатов по возрастанию id; end-to-end загрузка файла не выполнялась, чтобы не менять данные dev-базы, — сценарии покрыты юнит-тестами.

## Вне объёма (найдено попутно)
- Этап 3: в БД есть расходы с положительной суммой (импорт пишет сумму как в файле, `NormalizeSumSign` — при чтении) — знак для подбора кандидатов в переводы и валидации брать у нормализованного платежа, не из SQL.

## Итог
