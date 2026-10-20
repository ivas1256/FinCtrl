# План: [0012] Чистка мусора в рабочем backend-коде

> Статус: `в работе`. План сверен с кодом на старте (после 0011): номера строк из аудита сместились, состав мусора совпал.

## Затрагиваемые части
- backend: `FinCtrl.Website` (Program, Startup, appsettings.json, launchSettings, контроллеры PaymentsApi/PaymentSourcesApi, csproj), `FinCtrl.Application` (импорт, статистика, сервисы платежей/источников/тегов, csproj), `FinCtrl.Infrastructure` (репозитории платежей/источников, DbConnectionFactory), `FinCtrl.Domain`, `FinCtrl.Common`, `db/migrations/README.md`;
- тесты: `tests/FinCtrl.Application.Tests/PaymentsImport/FileFormats/`;
- frontend не затронут.

## Шаги
- [x] Мусорный код: `struct Test`/`CsvMap`, `catch { throw; }` и второй `LicenseContext` (оставлен в `XlsxFileFormat`), `CategoryTree.cs`, `DateRange.Intersect`, `var t` и `using static ...JSType`, закомментированный код (Program, Startup, PaymentsImportService, PaymentSourceRepository, GetPaymentsTableResponse, launchSettings), неиспользуемый `Program.OnConfiguring`
- [x] Неиспользуемые члены интерфейсов: `IPaymentService`/`IPaymentRepository.Exists(sum, date)`, `IPaymentSourceService`/`IPaymentSourceRepository.Delete`, `ITagService.GetByName`, `PeriodDto.ChildrenSum`
- [x] Переименование `PaymentSourceesApi` → `PaymentSourcesApi`, `partial` у `PaymentsApiController`, лишние `using` (`dotnet format style --diagnostics IDE0005`)
- [x] Пакеты: CsvHelper (Website, Application), `Microsoft.AspNetCore.OpenApi`, `System.Text.Json`, `System.Linq.Async`; `.config/dotnet-tools.json`
- [x] Startup/CORS: без `AddConsole`/`AddDebug`, Swagger «FinCtrl API», `Cors:AllowedOrigins` в `appsettings.json`
- [x] README миграций: `0003_tags.sql`
- [x] Тесты парсеров импорта (CSV, XLSX)
- [x] Сборка и ручная проверка
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [x] PR

## Тесты
- `CsvFileFormatTests.GetFileNodes_FileWithHeader_SkipsHeaderAndMapsColumns` — пропуск заголовка, маппинг колонок, поле в кавычках с `;`.
- `XlsxFileFormatTests` — маппинг колонок выписки и чтение до первой пустой даты; файл только с заголовком → пустой список (файл генерируется EPPlus в памяти).
- Удалённые члены и правки Startup поведения не меняют — покрываются сборкой и ручной проверкой.

## Ручная проверка
- `dotnet build FinCtrl.Backend.sln`: 0 ошибок, предупреждений 24 → 17 (оставшиеся — в файлах, которые задача не меняла).
- `dotnet test FinCtrl.Backend.sln`: Domain 8/8, Application 14/14.
- Preview `backend` + `frontend`: страницы Главная, Дашборд, Операции, Источники, Категории, Счета, Теги открываются с данными, ошибок в консоли браузера и в логах backend нет.
- API: все GET-маршруты (`AccountsApi`, `CategoriesApi/getAll|getTree`, `DashboardApi/summary|chart`, `PaymentsApi?periodType=Month`, `PaymentsApi/list`, `PaymentsApi/by-source/1`, `PaymentSourcesApi`, `PaymentSourcesApi/list`, `TagsApi`) → 200; `/swagger/v1/swagger.json` → title «FinCtrl API»; CORS: `Origin: http://localhost:5173` → `Access-Control-Allow-Origin`, чужой origin → без заголовка.
- Импорт: `POST api/PaymentsApi/upload` с тестовыми XLSX (новый счёт и источник) и CSV → платежи созданы с верными датой/суммой/источником/счётом; повторная загрузка XLSX дублей не создала. Тестовые платежи, источники и счёт затем удалены — количества строк и максимальные id `Payments`/`PaymentSources`/`Accounts` совпадают с исходными (сдвинулись только identity-счётчики).

## Риски и открытые вопросы
- Удаление `childrenSum` из ответа `GET api/PaymentsApi` — фронтенд поле не читает (проверено grep'ом), значение всегда было 0.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: `appsettings.Production.json` (строка подключения к prod-базе, без секретов, Windows-auth) по решению пользователя остаётся в git — не удалять и не выносить.
- 2026-10-20: стек веток по решению пользователя — ветка задачи создана от `refactor/0011-remove-legacy-mvc` (PR #14, не смёржен), а не от `main` (отступление от п.1.2 регламента); PR — в `refactor/0011-remove-legacy-mvc`, мёржить после #14. Дифф `main...HEAD` в хуке включает изменения предыдущих задач стека — ожидаемо.
- 2026-10-20: `LicenseContext` оставлен только в конструкторе `XlsxFileFormat` — EPPlus используется только там, `FileFormatFactory` создаёт формат перед чтением файла.
- 2026-10-20: в `CsvFileFormat` `string[] fields` → `var fields` + проверка `fields is null` (убирает CS8600/CS8602 в правленом файле; `ReadFields` возвращает null только в конце данных — поведение не меняется).
- 2026-10-20: лишние `using` удалены автоматически `dotnet format style --diagnostics IDE0005` (через временный `.editorconfig`, в репозиторий не попал). В файлах, затронутых только этим, хук требует отсутствия предупреждений — поэтому в `DbConnectionFactory._connection` и `MainStatisticPayment.PaymentDescription` добавлены nullable-аннотации (`?`), без изменения поведения.
- 2026-10-20: CORS-origins — простая секция `Cors:AllowedOrigins` (массив строк) в общем `appsettings.json`, чтобы сохранить прежнее поведение во всех окружениях; Options-класс ради одного массива не заводился.
- 2026-10-20: `RepositoryBase.FirstOrDefault` после удаления `PaymentRepository.Exists` не используется, но оставлен: это общий хелпер `src/common` (часть API базового класса, упомянут в документации).
- 2026-10-20: кандидаты от предыдущих задач: `Exists(sum, date)`, `IPaymentSourceService.Delete`, `dotnet-tools.json` — включены (входят в постановку). `Nullable`/`ImplicitUsings` в `FinCtrl.Website` и CS8632 в `PaymentEditDto` — не включены: это постановка задачи 0013. Остальные давние nullable-предупреждения — в файлах, которые задача не меняет, — во «Вне объёма».
- 2026-10-20: тест «CSV только с заголовком → пустой список» не добавлен: на таком файле импорт падает (см. «Вне объёма»), а менять поведение задача не должна.
- 2026-10-20: импорт проверен через API (`POST api/PaymentsApi/upload`, тот же endpoint, что у кнопки «Загрузить файл»), а не через диалог выбора файла в браузере — системный диалог недоступен для автоматизации.

## Вне объёма (найдено попутно)
- `CsvFileFormat`: заголовок пропускается по `parser.LineNumber == 2`; у файла из одной строки-заголовка без завершающего перевода строки `LineNumber` уже `-1`, заголовок не пропускается и импорт падает на `DateTime.Parse`. Кандидат в 0017 (ошибки/валидация) или отдельный fix.
- `GET api/PaymentsApi/list` без `pageSize` (все платежи) на dev-базе упирается в SQL-таймаут 30 с → 500, и пока запрос висит, остальные запросы того же клиента ждут. SPA всегда передаёт `pageSize`, так что на UI не влияет. Кандидат в 0015/0018.
- `GET api/CategoriesApi/getTree` отвечает за ~1–5 с (N+1 при построении дерева, известный техдолг из аудита).
- 17 давних предупреждений сборки в незатронутых файлах: CS8618 в `Domain/Dashboard/DashboardSummary.cs`, `Domain/Statistic/MainStatisticGroupItem.cs`, `Infrastructure/Payments/PaymentDto.cs` (+ CS8601/CS8629 в его `MapToPayment`), CS8632 в `Website/RequestDto/PaymentEditDto.cs` (уйдёт с включением Nullable в 0013). Предлагается закрыть в 0013 вместе с включением Nullable в Website.
- `.claude/hooks/pre-pr-check.mjs` всё ещё исключает `wwwroot/lib/` (отмечено ещё в 0011).

## Итог
Сделано: удалены черновики, отладочный и закомментированный код, неиспользуемые классы и члены интерфейсов, лишние `using` и NuGet-пакеты, `dotnet-ef`; папка `PaymentSourceesApi` переименована; `Startup` почищен, CORS-origins вынесены в `appsettings.json`, Swagger переименован; README миграций дополнен `0003_tags.sql`; добавлены тесты парсеров CSV/XLSX. Поведение API не изменилось, кроме пропавшего всегда нулевого поля `childrenSum`. Обновлены `project-summary.md` и `CLAUDE.md`. Осталось: пункты «Вне объёма».
