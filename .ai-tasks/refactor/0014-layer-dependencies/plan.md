# План: [0014] Исправить направление зависимостей между слоями

> Статус: `в работе`. План сверен с кодом после задач 0005–0013 (2026-10-20).

## Состояние кода на старте
- `FinCtrl.Application.csproj` ссылается на `FinCtrl.Infrastructure` и на пакет EPPlus.
- Пять интерфейсов `I*Repository` лежат в `Infrastructure/<Фича>/`, в контрактах только доменные типы (`Account`, `Category`, `Payment`, `PaymentSource`, `Tag`, `DateRange`) — `PaymentDto`/`PaymentSourceRow` в контракт не входят, остаются `internal` в Infrastructure.
- `using FinCtrl.Infrastructure.*` — в 5 сервисах Application и в `PaymentSourceServiceTests`.
- `PaymentsImport/FileFormats/{IFileFormat, FileFormatFactory, XlsxFileFormat, CsvFileFormat}` — в Application; `PaymentsImportService` зависит от конкретного класса `FileFormatFactory`.
- Тесты форматов (`CsvFileFormatTests`, `XlsxFileFormatTests`) — в `FinCtrl.Application.Tests`.
- Website: `using FinCtrl.Infrastructure` только в `Startup` (ради `AddInfrastructure()`), контроллеры с репозиториями не работают.

## Затрагиваемые части
- `Application/*.csproj`, `Infrastructure/*.csproj`, `FinCtrl.Backend.sln`
- `Infrastructure/*/I*Repository.cs` → `Application/<Фича>/` (namespace `FinCtrl.Application.<Фича>`)
- `Application/PaymentsImport/FileFormats/{Xlsx,Csv}FileFormat.cs`, `FileFormatFactory.cs` → `Infrastructure/PaymentsImport/`; в Application — `IFileFormat` и новый `IFileFormatFactory`
- DI: `AddApplication` (убрать фабрику), `AddInfrastructure` (добавить `IFileFormatFactory`)
- Тесты: новый проект `tests/FinCtrl.Infrastructure.Tests/` (тесты форматов и фабрики), `PaymentsImportServiceTests` в Application.Tests

## Шаги
- [ ] Перенос интерфейсов репозиториев в Application (+ XML-doc, file-scoped namespace)
- [ ] Разворот ссылок: Application ↛ Infrastructure, Infrastructure → Application; EPPlus — в Infrastructure
- [ ] Перенос FileFormats в Infrastructure, `IFileFormatFactory` в Application, DI
- [ ] Тестовый проект `FinCtrl.Infrastructure.Tests` (перенос тестов форматов, тесты фабрики), тесты `PaymentsImportService`
- [ ] Сборка, тесты
- [ ] Импорт xlsx/csv через API вручную, смоук страниц и API (preview `backend` + `frontend`)
- [ ] Документация (`backend-guide.md`, `project-summary.md`, `CLAUDE.md`, `architecture-audit.md`)
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
- `FinCtrl.Infrastructure.Tests/PaymentsImport/`: перенесённые `CsvFileFormatTests`, `XlsxFileFormatTests`; новые `FileFormatFactoryTests` (xlsx/csv, регистр расширения, неизвестный формат → исключение).
- `FinCtrl.Application.Tests/PaymentsImport/PaymentsImportServiceTests` (моки сервисов и `IFileFormatFactory`): новый платёж создаётся с источником/счётом и типом; дубль без счёта дозаполняется счётом; дубль со счётом не трогается; формат выбирается по расширению имени файла.
- `PaymentSourceServiceTests` — мокают интерфейс из Application.

## Ручная проверка
- preview `backend` + `frontend`: открыть все страницы (Главная, Dashboard, Операции, Категории, Источники, Счета, Теги) — без ошибок в консоли и логах.
- API через Swagger/curl: GET-списки всех контроллеров.
- Импорт через `POST api/PaymentsApi/upload` небольшим xlsx и csv; затем удалить созданные тестовые платежи/источники/счета из dev-базы (вернуть к исходному состоянию).

## Риски и открытые вопросы
- `PaymentDto` из Infrastructure используется в контракте репозитория — **снято**: в контрактах только доменные типы, `PaymentDto` остаётся внутренним маппингом Infrastructure.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: по выбору пользователя («стек веток») ветка создана от `chore/0013-backend-net9-packages` (PR #16, не смёржен), а не от `main` — отступление от п. 1.2 регламента. PR — в `chore/0013-backend-net9-packages`, мёржить после #16. Дифф `main...HEAD` для хука включает изменения 0005–0013 — ожидаемо.
- 2026-10-20: фабрика форматов — интерфейс `IFileFormatFactory` в `Application/PaymentsImport/FileFormats/` рядом с `IFileFormat`, реализация `FileFormatFactory` (internal) и форматы — в `Infrastructure/PaymentsImport/` (как в постановке), регистрация в `AddInfrastructure()`. `Xlsx/CsvFileFormat` стали `internal` (наружу не нужны), тесты видят их через `InternalsVisibleTo`.
- 2026-10-20: тесты форматов переехали в новый проект `tests/FinCtrl.Infrastructure.Tests` (по гайду §8 — тест-проект на тестируемый проект).
- 2026-10-20: `NotImplementedException` для неизвестного формата не меняем — это объём 0017. Реализация репозиториев (sync/async, SELECT'ы) не трогается — 0015.
- 2026-10-20: базовые тесты `PaymentsImportService` добавлены здесь, т.к. сервис затронут (зависимость от фабрики стала интерфейсом и впервые мокается). 0016 их расширит при переделке импорта (транзакции, логи).
- 2026-10-20: перенесённые интерфейсы репозиториев переведены на file-scoped namespace и получили XML-doc (гайд §7 — файл правится в задаче); сигнатуры не менялись.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
