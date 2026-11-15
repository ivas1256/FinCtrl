# Миграции БД

Просто папка с SQL-файлами (диалект PostgreSQL), без ORM и без раннера. Таблицы
учёта применённых миграций нет — что применено, видно по названию файла и истории git.

## Соглашение

```
000N_короткое_описание.sql
```

Номер — сквозной, по порядку. Не переиспользовать номера и не менять задним
числом уже применённые файлы — вместо правки старого файла добавляется новый.
Миграция идемпотентна: `CREATE TABLE/INDEX IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`
и т.п. Имена таблиц и колонок — без кавычек (PostgreSQL хранит их в нижнем регистре).

## Файлы

- `0001_initial.sql` — baseline-схема PostgreSQL по состоянию на 2026-11-15
  (задача 0041): Categories, PaymentSources, Accounts, Payments, Tags, PaymentTags,
  Transfers — таблицы, PK/FK, индексы (в т.ч. частичные уникальные), default.
  Итог T-SQL-миграций 0001–0006 времён SQL Server.

T-SQL-миграции 0001–0007 (MS SQL Server, до задачи 0041) — архив `../migrations-mssql/`,
не применяются. Перенос данных из SQL Server — `../mssql-to-postgres/Copy-MssqlToPostgres.ps1`.

## Как накатить

Из `FinCtrl/`, пароль роли `finctrl` — в `%APPDATA%\postgresql\pgpass.conf`:

```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5433 -U finctrl -d finctrl_db_dev2 -v ON_ERROR_STOP=1 -f db/migrations/000N_....sql
```
