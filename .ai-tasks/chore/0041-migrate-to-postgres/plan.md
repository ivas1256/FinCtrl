# План: [0041] Перенести БД с MS SQL Server на PostgreSQL

## Затрагиваемые части
- БД: `db/migrations/0001_initial.sql` (новый baseline под PostgreSQL), архив `db/migrations-mssql/0001–0007`, скрипт переноса `db/mssql-to-postgres/`
- Backend: `FinCtrl.Common.Infrastructure` (`IDbConnectionFactory`, `RepositoryBase`, csproj), `FinCtrl.Infrastructure` (`DbConnectionFactory`, все репозитории), `FinCtrl.Website/appsettings.Development.json`, тесты Infrastructure при необходимости
- Frontend: нет. Контракт API не меняется.

## Шаги
- [x] 1. Перенести T-SQL-миграции в `db/migrations-mssql/`, написать PostgreSQL-baseline `db/migrations/0001_initial.sql` (идемпотентный: `IF NOT EXISTS`)
- [x] 2. Npgsql вместо Microsoft.Data.SqlClient в фабрике соединений и транзакциях
- [x] 3. Переписать T-SQL в репозиториях (`RETURNING`, `LIMIT`, `false/true`, без `dbo.`, `= ANY(@Ids)` без разбивки на пачки)
- [x] 4. Проверить, что ошибки БД не перехватываются внутри транзакции с продолжением работы
- [x] 5. Строка подключения Development → PostgreSQL (без пароля в репозитории: пароль — `pgpass.conf`)
- [x] 6. Скрипт переноса данных (PowerShell: чтение через System.Data.SqlClient → CSV → `psql \copy`), перенос `FinCtrl_db_dev2` и `FinCtrl_db`, сверка
- [x] 7. Документация: `CLAUDE.md`, `project-summary.md`, `backend-guide.md`, README миграций
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [x] PR — https://github.com/ivas1256/FinCtrl/pull/44

## Тесты
| Что тестируем | Сценарии |
|---------------|----------|
| `PaymentRepositoryTests` | алиасы `SelectPayment` совпадают со свойствами `PaymentDto` (существующий тест, должен остаться зелёным) |

SQL репозиториев юнит-тестами не покрывается (регламент, раздел 3) — проверка вручную на dev-базе PostgreSQL.

## Ручная проверка
На `finctrl_db_dev2` в PostgreSQL: главная (тепловая карта), «Операции» (пагинация, фильтр по тегам, поиск источников), правка платежа (теги, перевод, счёт), импорт файла (get-or-create источников/счетов, дубли), создание/удаление тега и счёта.

## Риски и открытые вопросы
- Сравнение строк теперь с учётом регистра: поиск источника `LIKE` и get-or-create по имени чувствительны к регистру (принято пользователем).
- SQL Server при `=` игнорирует хвостовые пробелы, PostgreSQL — нет.
- PostgreSQL прерывает транзакцию после любой ошибки до отката.
- `appsettings.Production.json` вне PR — см. «Вне объёма» в `about.md`.

## Журнал решений
- 2026-11-15: имена без кавычек (нижний регистр в PG) — минимум правок SQL; `timestamp` без зоны — как `DATETIME2`, `DateTime` с `Kind=Unspecified` работает без конвертации.
- 2026-11-15: регистр сравнения — по умолчанию PostgreSQL, без доп. мер (решение пользователя).
- 2026-11-15: в динамическом ORDER BY списка операций — NULLS FIRST (ASC) / NULLS LAST (DESC), как в SQL Server (находка backend-dev); `AddTagsToPayments` — `ON CONFLICT DO NOTHING`.
- 2026-11-15: перенос данных — PowerShell + `psql \copy`: не нужны pgloader и дополнительные драйверы.
- 2026-11-15: PostgreSQL 16 удалён пользователем, PostgreSQL 18 переведён на стандартный порт 5432 (по подтверждению пользователя: postgresql.conf, pgpass.conf, строка подключения и документация).
- 2026-11-15: прод-база SQL Server `FinCtrl_db` оказалась устаревшей копией (схема времён EF без Accounts/Tags/Transfers/IsDeleted, 3119 платежей до 2024-03; 3116 из них есть в dev2, ещё 3 — мусор с датой в 9999 году). По решению пользователя `finctrl_db` заполнена копией `FinCtrl_db_dev2`.
- 2026-11-15: перенос выполнен: `FinCtrl_db_dev2` → `finctrl_db_dev2` и `FinCtrl_db_dev2` → `finctrl_db`; сверка сошлась по всем 7 таблицам (Categories 25, PaymentSources 1335, Accounts 10, Payments 7091, Tags 2, PaymentTags 18, Transfers 136; сумма PaymentSum 8857852.12), кириллица и даты проверены выборочно, счётчики identity — за максимальным id.

## Вне объёма (найдено попутно)
-

## Итог
- Backend переведён на PostgreSQL 18 (Npgsql 10.0.3): baseline-схема `db/migrations/0001_initial.sql`, T-SQL-миграции — архив `db/migrations-mssql/`, весь SQL репозиториев — диалект PostgreSQL; порядок NULL в сортировке списка операций сохранён как в SQL Server.
- Данные перенесены скриптом `db/mssql-to-postgres/Copy-MssqlToPostgres.ps1` в `finctrl_db_dev2` и `finctrl_db` (обе — из `FinCtrl_db_dev2`, см. журнал решений), сверка по 7 таблицам сошлась.
- Проверено: `check.mjs backend` — сборка OK, тесты 438/438. Вручную на `finctrl_db_dev2` (backend + frontend): главная (тепловая карта, запросы 200), «Операции» (пагинация, сортировка по дате/источнику/категории/сумме, фильтр по тегу), правка платежа (описание, ExcludeFromStats, теги — запись в БД подтверждена, затем откат), создание/удаление тега (связи чистятся), массовое назначение тегов (54 операции, повтор без дублей), кандидаты перевода, импорт CSV (новый платёж с id после максимального, новый источник; повторный импорт — дубль). Ошибок в логе backend и консоли браузера нет; тестовые записи удалены.
- Осталось пользователю: строка подключения Production к `finctrl_db` (хук запрещает менять `appsettings.Production.json`).
