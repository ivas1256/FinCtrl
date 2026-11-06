# План: [0038] Номер счёта у счетов и сопоставление при импорте по номеру

## Затрагиваемые части
- БД: миграция `0006_account_number.sql` (колонка `Accounts.AccountNumber`, уникальный фильтрованный индекс, снятие уникальности названия)
- Backend: Domain (`Account`, `ExcelPayment`), Infrastructure (`AccountRepository`, `XlsxFileFormat`, `CsvFileFormat`),
  Application (`AccountService`, `PaymentsImportService`), Website (`AccountDto`, `AccountsApiController`)
- Frontend: `src/pages/accounts/Accounts.vue`

## Шаги
- [ ] 1. Миграция 0006 + накат на dev-базу
- [ ] 2. Domain: `Account.AccountNumber`, фабрики; `ExcelPayment.InvoiceNumber` → `AccountNumber`
- [ ] 3. Infrastructure: репозиторий (`GetByAccountNumber`), xlsx колонка D, CSV без номера
- [ ] 4. Application: конфликт по номеру, валидация; get-or-create счёта по номеру в импорте
- [ ] 5. Website: `AccountDto.AccountNumber`, контроллер
- [ ] 6. Frontend: колонка/поле «Номер счёта»
- [ ] 7. Юнит-тесты
- [ ] 8. Сборка + тесты + ручная проверка (Definition of Done)
- [ ] 9. Документация, PR

## Тесты
| Что тестируем | Сценарии |
|---------------|----------|
| `AccountTests` | фабрики с номером, нормализация пустого номера |
| `AccountServiceTests` | конфликт по номеру (Create/Update), одинаковое название без конфликта, длина номера |
| `PaymentsImportServiceTests` | найден по номеру; дозаполнение номера legacy-счёту; создание с номером; без номера — по названию; кэш по номеру |
| `XlsxFileFormatTests` / `CsvFileFormatTests` | номер из колонки D / в CSV номера нет |
| `AccountsApiControllerTests` | маппинг `AccountNumber` |

## Ручная проверка
Preview `backend` + `frontend`: страница «Счета» — создать/изменить счёт с номером, дубль номера → ошибка.
Импорт xlsx: счёт без номера получает номер, повторный импорт не создаёт счетов (`SELECT` через `sqlcmd`).

## Риски и открытые вопросы
- Legacy-счёт ищется по названию среди счетов без номера; если таких несколько — берётся первый по `AccountId`.

## Журнал решений
- 2026-11-06: номер счёта — колонка D xlsx (подтвердил пользователь); CSV не трогаем.
- 2026-11-06: уникальность названия снята — ключ сопоставления теперь номер.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
