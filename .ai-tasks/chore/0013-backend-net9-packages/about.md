# [0013] Перевести backend на net9 и Microsoft.Data.SqlClient

- **Ветка:** `chore/0013-backend-net9-packages`
- **Репозитории:** backend
- **Создана:** 2026-10-20
- **Источник:** `.ai-docs/project/architecture-audit.md`, §2.1, §2.5

## Контекст
TargetFramework у проектов разный: Website — net9, Domain/Application/Infrastructure — net7 (EOL), Common — net6 (EOL). Используется legacy `System.Data.SqlClient`, в Website не включены `Nullable`/`ImplicitUsings`, версии `Microsoft.Extensions.*` расходятся.

## Постановка
- Все проекты .sln (и тестовые) перевести на `net9.0`.
- `System.Data.SqlClient` заменить на `Microsoft.Data.SqlClient` (в т.ч. `IDbConnectionFactory`).
- Выровнять версии пакетов.
- Включить `<Nullable>enable</Nullable>` и `<ImplicitUsings>enable</ImplicitUsings>` в Website, исправить предупреждения.

## Вне объёма
- Рефакторинг слоёв (0014).

## Критерии приемки
- [x] Все проекты на net9.0
- [x] Нет ссылок на `System.Data.SqlClient`
- [x] В изменённых файлах нет предупреждений nullable
- [x] Ручной прогон SPA: чтение и запись работают (строка подключения совместима с Microsoft.Data.SqlClient: `TrustServerCertificate`/`Encrypt`)
- [x] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
- [x] Все тесты зелёные
