# [0014] Исправить направление зависимостей между слоями

- **Ветка:** `refactor/0014-layer-dependencies`
- **Репозитории:** backend
- **Создана:** 2026-10-20
- **Источник:** `.ai-docs/project/architecture-audit.md`, §2.1

## Контекст
По гайду (§2) Infrastructure зависит от Application, и интерфейсы репозиториев объявляются в Application. В коде наоборот: `FinCtrl.Application.csproj` ссылается на Infrastructure, все 5 `I*Repository` лежат в `Infrastructure/<Фича>/`. Парсинг файлов (EPPlus, `TextFieldParser`) находится в Application, хотя это зона Infrastructure.

## Постановка
- Перенести `IAccountRepository`, `ICategoryRepository`, `IPaymentRepository`, `IPaymentSourceRepository`, `ITagRepository` (и их read-модели/DTO, если они нужны в контракте) в `Application/<Фича>/`.
- Удалить ссылку Application → Infrastructure, добавить Infrastructure → Application.
- Перенести `PaymentsImport/FileFormats/*` (Xlsx/Csv, фабрика) в `Infrastructure/PaymentsImport/`, интерфейс `IFileFormat`/фабрики оставить в Application; EPPlus — в Infrastructure.

## Вне объёма
- Изменение реализации репозиториев (0015/0016).

## Критерии приемки
- [x] `FinCtrl.Application.csproj` не ссылается на Infrastructure; в Application нет `using FinCtrl.Infrastructure`
- [x] Infrastructure ссылается на Application
- [x] Website ссылается на Infrastructure только ради `AddInfrastructure()`
- [x] Тесты сервисов мокают интерфейсы из Application; импорт файла работает (ручная проверка)
- [x] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
- [x] Все тесты зелёные
