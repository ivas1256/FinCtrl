# План: [0039] Счета перевода в списке операций на главной

## Затрагиваемые части
- БД: миграции нет
- Backend:
  - Infrastructure — `Payments/PaymentRepository.cs` (`SelectPayment`), `Payments/PaymentDto.cs` (+ `MapToPayment`)
  - Domain — `Payment.cs` (новое свойство `TransferPairAccount`)
  - Website — `Controllers/PaymentsApi/PaymentDto.cs` (`FromModel`)
- Frontend: `src/pages/paymentsTable/PaymentList.vue`, `src/pages/paymentsTable/PaymentsTable.vue` (подпись второй стороны в модалке)

## Контракт API
- `GET api/PaymentsApi` (и все ответы с `PaymentDto` Website): новое поле `transferPairAccount: AccountDto | null` — та же форма, что у `account` (`accountId`, `excelAccountName`, `displayName`, `isSavings`, `createdAt`, `lastUpdatedAt`). `null` — нет пары (`transferPairPaymentId == null`) или у второй стороны нет счёта. Новых маршрутов и кодов ошибок нет.

Backend — шаги 1–4, frontend — шаги 5–6.

## Шаги
- [x] 1. `SelectPayment`: `LEFT JOIN Payments pp` (вторая сторона — `CASE WHEN tr.OutPaymentId = p.PaymentId THEN tr.InPaymentId ELSE tr.OutPaymentId END`) и `LEFT JOIN Accounts pa ON pa.AccountId = pp.AccountId`; колонки `TransferPairAccount_*` (id, название, `IsSavings` и остальные поля счёта по образцу `Account_*`)
- [x] 2. Infrastructure `PaymentDto`: свойства `TransferPairAccount_*`, маппинг в `Payment.TransferPairAccount` (`Account?`, `null`, если id нет)
- [x] 3. Domain `Payment.TransferPairAccount`
- [x] 4. Website `PaymentDto.TransferPairAccount` (`AccountDto?`) в `FromModel`
- [x] 5. `PaymentList.vue`: у перевода строка «A → B» (по знаку нормализованной суммы: списание — свой счёт → счёт пары; поступление — наоборот), «не указан» при отсутствии; логику вынести в чистую функцию в `src/utils/` и покрыть тестом
- [x] 6. `PaymentsTable.vue`: подпись текущей второй стороны, не найденной среди кандидатов, — с её счётом
- [x] Юнит-тесты (см. таблицу)
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] Обновить `project-summary.md`
- [ ] PR

## Тесты
| Что тестируем | Сценарии |
|---------------|----------|
| `PaymentRepositoryTests` | алиасы `SelectPayment` ↔ `PaymentDto` (существующий тест ловит новые колонки) |
| Infrastructure `PaymentDtoTests` | `MapToPayment`: со счётом пары → `TransferPairAccount` заполнен; без id счёта пары → `null` |
| Website `PaymentDtoTests` | `FromModel`: `TransferPairAccount` маппится / `null` |
| Функция направления перевода (frontend utils) | списание, поступление, перевод без пары, платёж без счёта |
| `PaymentList.spec.js` | у перевода выводится строка «A → B», у обычного платежа — нет |

## Ручная проверка
- preview `backend` + `frontend`: главная → клик по ячейке «Переводы» → `read_page`/`get_page_text`: у списания и поступления правильные «A → B», у непарного — «не указан».
- `read_network_requests`: в ответе `GET /api/PaymentsApi` у связанного перевода есть `transferPairAccount`.
- Модалка правки перевода: подпись второй стороны со счётом.
- Нет ошибок в консоли и логах backend.

## Риски и открытые вопросы
- Ветка 0038 (номер счёта) ещё на ревью и тоже меняет `SelectPayment`/счета. Ветка 0039 создана от `main`, поэтому после мёржа 0038 возможен небольшой конфликт в `SelectPayment` и DTO — разрешить при синхронизации.
- Знак суммы брать у нормализованного платежа (в БД есть расходы с положительной суммой).

## Журнал решений
- 2026-11-07: счёт второй стороны берётся на backend (JOIN в `SelectPayment`), а не поиском пары среди загруженных платежей на frontend: пара может быть в другом периоде, вне диапазона, в «Накоплениях» или отфильтрована по тегам.
- 2026-11-07: подпись второй стороны в модалке правки делается в этой задаче — это одна-две строки на том же новом поле.
- 2026-11-07: ветка создана от `main` по регламенту, не от ветки 0038.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении: что сделано, что отложено.
