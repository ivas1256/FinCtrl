# [0012] Чистка мусора в рабочем backend-коде

- **Ветка:** `chore/0012-backend-code-cleanup`
- **Репозитории:** backend
- **Создана:** 2026-10-20
- **Источник:** `.ai-docs/project/architecture-audit.md`, §1.3

## Контекст
В рабочем коде остались черновики, отладочный код, закомментированные блоки, неиспользуемые классы, члены интерфейсов и NuGet-пакеты. Гайд (§7) это прямо запрещает.

## Постановка
- Удалить: `struct Test` со `static Main`, `CsvMap` (`CsvFileFormat.cs`); `catch { throw; }` и двойной `LicenseContext` (`XlsxFileFormat.cs`); `Domain/CategoryTree.cs`; `DateRange.Intersect`; `var t` и `using static ...JSType` (`MainStatisticService.cs`); закомментированный код (`Program.cs`, `Startup.cs`, `PaymentsImportService.cs`, `PaymentSourceRepository.cs`, `GetPaymentsTableResponse.cs`, `launchSettings.json`); неиспользуемые члены `Exists`, `IPaymentSourceService/Repository.Delete`, `ITagService.GetByName`, `PeriodDto.ChildrenSum`.
- Переименовать папку и namespace `Controllers/PaymentSourceesApi` → `PaymentSourcesApi` (URL не меняется); убрать `partial` у `PaymentsApiController`; убрать лишние `using`.
- Пакеты: удалить CsvHelper (Website и Application), `Microsoft.AspNetCore.OpenApi`, явный `System.Text.Json`, `System.Linq.Async`; удалить `.config/dotnet-tools.json` (`dotnet-ef`).
- `Startup`: убрать `AddConsole`/`AddDebug` перед `ClearProviders`; Swagger — имя «FinCtrl API»; CORS-origins вынести в конфигурацию (`appsettings`).
- `db/migrations/README.md`: добавить `0003_tags.sql`.

## Вне объёма
- Смена TFM и SqlClient (0013).
- Изменение поведения.
- `appsettings.Production.json` — по решению пользователя остаётся в git как есть (строка подключения без секретов, Windows-auth); не удалять и не переносить.

## Критерии приемки
- [x] Все перечисленные элементы удалены или исправлены
- [x] Сборка без ошибок и без новых предупреждений
- [x] Ручной прогон SPA: главная, Операции, Источники, импорт файла
- [x] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
- [x] Все тесты зелёные
