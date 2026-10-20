# FinCtrl — саммари проекта (актуально на 2026-09-17)

Личное приложение для учёта финансов. Основной сценарий: пользователь
выгружает из банка Excel/CSV со списком операций, приложение их импортирует,
раскладывает по источникам платежа / счетам / категориям и показывает
сводные таблицы.

Документ собран по итогам исследования кодовой базы и доработок (фича
"Счета"). Можно прикладывать целиком к новым запросам как контекст о
проекте — актуализировать по мере изменений.

## Структура репозитория

Один git-репозиторий в корне (`root.FinCtrl`, базовая ветка `main`), две части:

- `FinCtrl/` — backend, .NET 9 + MS SQL Server.
- `FinCtrl.Frontend/FinCtrl.Frontend/` — фронтенд, Vue 3 + Vite.

### Backend: проекты

Все backend-проекты `FinCtrl/` входят в `FinCtrl.Backend.sln`:
- `FinCtrl.Website` — единственное исполняемое приложение в решении
  (`FinCtrl.Backend.sln`). ASP.NET Core, только REST API
  (`*ApiController`, `AddControllers` + `MapControllers`); UI — Vue SPA.
  Legacy MVC/Razor-фронтенд удалён задачей 0011.
  Program/Startup регистрирует `AddApplication()` + `AddInfrastructure()`,
  CORS разрешает Vue dev-серверы из `appsettings.json` → `Cors:AllowedOrigins`
  (`localhost:3000/5173/5174/8080`).
- `src/main/FinCtrl.Domain` — сущности (без EF, POCO; `Payment` пока `record`, что нарушает гайд, — см. `architecture-audit.md`).
- `src/main/FinCtrl.Application` — сервисы (бизнес-логика), интерфейсы репозиториев и форматов импорта; на Infrastructure не ссылается.
- `src/main/FinCtrl.Infrastructure` — реализации репозиториев (Dapper, raw SQL) и парсинг файлов импорта; ссылается на Application.
- `src/common/FinCtrl.Common`, `FinCtrl.Common.Infrastructure` —
  общий `RepositoryBase`, `IDbConnectionFactory`, `DateRange`.

Других backend-проектов в репозитории нет: заброшенная попытка
переписать бэкенд на EF Core ("clean architecture" rewrite вне `.sln`)
удалена задачей 0010.

**Доступ к данным:** без ORM, чистый Dapper поверх Microsoft.Data.SqlClient. Все запросы — raw SQL
внутри репозиториев. `RepositoryBase` даёт только хелперы
`Execute/Query/FirstOrDefault`. Dommel/FluentMap в коде нет, раньше
документация упоминала их по ошибке.

**БД:** MS SQL Server, дев-строка подключения в
`FinCtrl.Website/appsettings.Development.json`:
`Server=Ivan; Database=FinCtrl_db_dev2` (Windows-аутентификация,
локальный сервер на машине пользователя).

**Миграции:** `FinCtrl/db/migrations/*.sql` — просто папка SQL-файлов,
без раннера и без таблицы учёта применённых миграций. Конвенция:
`000N_короткое_описание.sql`, номера сквозные, накатывать вручную:
```
sqlcmd -S <сервер> -d <база> -E -C -i db\migrations\000N_....sql
```
Подробности — `db/migrations/README.md`. Файлы на сейчас:
- `0001_initial.sql` — базовая схема (Categories, PaymentSources, Payments).
- `0002_accounts.sql` — таблица `Accounts` + колонка `Payments.AccountId`.

## Доменная модель

Приоритет категории: **категория платежа > категория источника платежа**.
Если у платежа своя категория не задана — используется категория его
источника (`Payment.CalculatedCategory = PaymentCategory ?? PaymentSource?.PaymentSourceCategory`).

- **Payment** (`src/main/FinCtrl.Domain/Payment.cs`, `record`) —
  `PaymentId, PaymentType (enum Spending/Income), PaymentSum, PaymentDate,
  PaymentSource, Account, PaymentCategory, PaymentDescription,
  ExcludeFromStats, CreatedAt, LastUpdatedAt`.
- **PaymentSource** ("Источник платежа") — по сути метка
  контрагента/строки банковской выписки (например конкретный магазин),
  **не то же самое, что банковский счёт**. Поля: `PaymentSourceId,
  PaymentSourceName, PaymentSourceCategory (nullable Category),
  Description, CreatedAt, LastUpdatedAt`. Создаётся автоматически при
  импорте (get-or-create по имени), CRUD-эндпоинт умеет только читать и
  редактировать (create/delete нет — источники берутся только из импорта).
- **Account** ("Счёт", добавлено 2026-09-08) — именно банковский счёт.
  Поля: `AccountId, ExcelAccountName (как называется в файле банка — ключ
  для get-or-create при импорте), DisplayName (что показывается в UI),
  CreatedAt, LastUpdatedAt`. В отличие от `PaymentSource`, здесь полный
  CRUD (создание/редактирование/удаление) через отдельную страницу.
- **Category** — дерево (`ParentCategoryId`), `IsOnlyIncome`,
  `OrderIndex`, `Color`.

## Таблицы БД (dbo)

- `Categories`: CategoryId PK, CategoryName, ParentCategoryCategoryId
  (self FK), IsOnlyIncome, OrderIndex, Color.
- `PaymentSources`: PaymentSourceId PK, PaymentSourceName, CategoryId FK,
  CreatedAt, LastUpdatedAt, Description.
- `Accounts`: AccountId PK, ExcelAccountName (уникальный индекс),
  DisplayName, CreatedAt, LastUpdatedAt.
- `Payments`: PaymentId PK, PaymentType, PaymentSum, PaymentDate,
  PaymentSourceId FK, AccountId FK → Accounts, PaymentCategoryCategoryId
  FK, PaymentDescription, CreatedAt, LastUpdatedAt, IsDeleted,
  ExcludeFromStats. Индексы на (PaymentDate, IsDeleted),
  PaymentCategoryCategoryId, PaymentSourceId, AccountId.

## Импорт из Excel/CSV

Точка входа: `POST api/PaymentsApi/upload` →
`PaymentsApiController.UploadFile` → `IPaymentsImportService.Upload`.
`IFileFormatFactory` (реализация `FileFormatFactory`, `Infrastructure/PaymentsImport/`) выбирает `XlsxFileFormat` (EPPlus) или
`CsvFileFormat` (`TextFieldParser`, разделитель `;`) по расширению файла.

**Маппинг колонок Excel** (1-индексация, т.е. номер = буква столбца,
`XlsxFileFormat.cs`):
| Колонка | Поле |
|---|---|
| 1 (A) | Дата |
| 3 (C) | Название счёта (для get-or-create Account) |
| 4 (D) | Номер операции |
| 7 (G) | Название источника платежа |
| 8 (H) | Сумма |
| 10 (J) | Статус |
| 13 (M) | Тип ("Списание"/"Пополнение") |

**Паттерн get-or-create** (используется одинаково и для `PaymentSource`,
и для `Account`): проверить `Exists(имя)` → если нет, создать и
перечитать по id; если есть — получить по имени
(`GetByName`/`GetByExcelName`). Это общий шаблон для любой
lookup-сущности, создаваемой автоматически при импорте — держаться его и
для будущих подобных сущностей.

**Дедупликация / дозаполнение:** для каждой строки файла ищется уже
существующий платёж с той же суммой и датой (`FindDuplicate`). Если не
найден — создаётся новый. Если найден и у него **ещё не проставлен**
`Account`, а в текущей строке файла счёт есть — он дозаполняется
(`Update`), без создания дубликата. Так повторная загрузка того же файла
безопасно проставляет счета старым операциям.

## Backend API (ключевые контроллеры, `FinCtrl.Website/Controllers`)

- **PaymentsApi** — `GET` (дерево статистики для главной страницы, с
  датами и группировкой), `PUT {id}` (правка платежа: категория / счёт /
  описание / `excludeFromStats`), `GET list` (пагинация/сортировка/
  фильтры — используется страницей "Операции"), `GET by-source/{id}`,
  `POST upload`.
  - **Важный нюанс редактирования**: `CategoryId == null` в запросе —
    категория не трогается (её нельзя явно очистить этим способом,
    старое ограничение). Для `AccountId` сделано иначе и корректнее:
    `null` — не трогать, конкретный id — установить, `0` — явно
    очистить. Если будете добавлять новые редактируемые поля — учитывайте
    это несоответствие в поведении.
- **PaymentSourcesApi** — `GET`, `GET list`, `PUT {id}` (правка одного источника, id из роута; с задачи 0006), `PUT` (legacy: id в теле, используется массовым сохранением); только правка,
  create/delete нет.
- **AccountsApi** — полный CRUD: `GET`, `POST`, `PUT {id}`, `DELETE {id}`.
- **CategoriesApi** — дерево категорий и т.п.

**Грабля, на которую уже наступали:** в `PaymentRepository` информация о
платеже собирается двумя раздельно написанными SQL-блоками:
`_selectPaymentInfo` (общий, используется в `Get/GetByDateRange/
GetBySource/FindDuplicate`) и отдельный inline `dataSql` внутри
`GetListPaged`. Это два разных места, которые надо синхронно обновлять —
при первой доработке колонку `Account` забыли добавить именно в
`GetListPaged`, из-за чего страница "Операции" не показывала счёт, хотя
в БД он был. **При добавлении новых полей к Payment — искать оба места.**

## Frontend (`FinCtrl.Frontend/FinCtrl.Frontend`, Vue 3 + Vite)

- `config.js` → `APP_CONFIG.apiBaseUrl = http://localhost:5000`.
- `index.js` — роуты: `/` (главная), `/dashboard`, `/payments`
  ("Операции"), `/payment-sources`, `/categories`, `/accounts`.
- `src/App.vue` — левое меню-сайдбар со ссылками на все роуты.
- Страницы (`src/pages/`):
  - `paymentsTable/PaymentsTable.vue` — **главная страница**: тепловая
    карта категория×период, клик по ячейке показывает список платежей за
    неё (`PaymentList.vue` + `PaymentSourcesList.vue`), модалка правки
    одного платежа (категория, счёт — select, описание), сохранение через
    `PUT api/PaymentsApi/{id}`.
  - `payments/Payments.vue` — **"Операции"**: полноценная таблица с
    пагинацией/сортировкой/фильтрами, инлайн-правка категории и описания,
    загрузка файла, столбец "Счёт" (пока только отображение).
  - `paymentSources/PaymentSources.vue` — правка категории/описания у
    источников платежа (без создания/удаления).
  - `accounts/Accounts.vue` — полноценный CRUD для счетов (создание,
    инлайн-правка, удаление).
  - `categories/Categories.vue`, `dashboard/Dashboard.vue`.

## История доработок в этой сессии (2026-09-08 — 2026-09-17)

1. Добавлена фича "Счета": миграция `0002_accounts.sql`, домен/сервис/
   репозиторий `Account`, `AccountsApiController` (полный CRUD),
   Vue-страница `Accounts.vue` + роут + пункт меню.
2. Импорт из Excel читает столбец C как название счёта, привязывает через
   get-or-create; повторная загрузка файла дозаполняет счета у уже
   существующих операций без дублей.
3. Столбец "Счёт" добавлен на страницу "Операции"; редактируемый select
   "Счёт" добавлен в форму правки платежа на главной странице.
4. Найден и исправлен баг: `GetListPaged` не джойнил `Accounts` (см.
   "грабля" выше) — из-за этого счёт не отображался в "Операциях" даже
   при наличии данных в БД.

## На заметку на будущее

- `db/migrations/` на момент 2026-09-08 не был закоммичен в git — стоит
  проверить/закоммитить.
- При добавлении новых полей к `Payment` — не забывать `PaymentDto`
  (Infrastructure и Website), `MainStatisticPayment` (используется
  деревом статистики на главной) и все SQL-выборки в `PaymentRepository`.

## Фича "Теги" (добавлено 2026-09-17)

Первая many-to-many связь в проекте (`Tags` + `PaymentTags`,
`db/migrations/0003_tags.sql`), без каскадного удаления на уровне БД
(только `NO ACTION`) — `TagRepository.Delete` сначала вручную чистит
строки `PaymentTags`, затем сам тег.

- Backend: `FinCtrl.Domain.Tag`, `ITagRepository` (`src/main/FinCtrl.Application/Tags`)/`TagRepository`
  (`src/main/FinCtrl.Infrastructure/Tags`), `ITagService`/`TagService`
  (`src/main/FinCtrl.Application/Tags`), `TagsApiController` (полный CRUD,
  по образцу `AccountsApiController`).
- `Payment.Tags` (домен) и `MainStatisticPayment.Tags` заполняются не через
  SQL join в `_selectPaymentInfo`/`dataSql` (это два хрупких места, см.
  выше), а отдельным батч-запросом `PaymentRepository.GetTagsForPayments` +
  `AttachTags`, вызываемым после основной выборки в `GetByDateRange` (кормит
  главную страницу через дерево статистики) и `GetListPaged` (кормит
  "Операции"). Фильтр по тегам — SQL `EXISTS`-подзапрос в `GetListPaged`
  (там пагинация), и обычный LINQ-фильтр в памяти в `GetByDateRange` (там
  весь период грузится целиком для дерева).
  Оба пути дальше независимо мапятся в один и тот же Website-слойный
  `PaymentDto` (`Controllers/PaymentsApi/PaymentDto.cs`) — при доработке
  тегов/новых agregate-полей у платежа проверять оба места мэппинга внутри
  `PaymentsApiController` (`GetList` и `BuildTree`).
- Массовое назначение тегов — `POST api/PaymentsApi/bulk-tags`
  (`PaymentService.BulkAssignTags`): фильтр за период + список категорий
  (без учёта потомков), назначение **аддитивное** (не стирает уже
  проставленные теги).
- `PaymentEditDto.TagIds`: `null` — не трогать теги, список (в т.ч. пустой)
  — заменить набор целиком. Такая же по духу конвенция, как у `AccountId`
  (в отличие от более старого и однозначно кривого поведения `CategoryId`,
  который трогать нельзя).
- Frontend: подключена библиотека `vue-multiselect` (единственная
  сторонняя UI-зависимость сверх Bootstrap/Chart.js) — обёртка
  `src/components/TagMultiSelect.vue` умеет и мультивыбор, и создание
  нового тега прямо из поля ввода (`taggable`). Новая страница
  `src/pages/tags/Tags.vue` (CRUD, по образцу `Accounts.vue`), поле тегов в
  модалке правки платежа на главной, колонка/фильтр тегов и модалка
  массового назначения на странице "Операции".
- Попутно найден и исправлен **предсуществующий баг**: инлайн-сохранение
  на странице "Операции" (`saveSinglePayment`/`saveAllChanges` в
  `Payments.vue`) не отправляло `paymentId` в теле PUT-запроса. Так как
  `PaymentsApiController.EditPayment` помечен `[FromBody]` целиком,
  `[FromRoute]` на отдельном свойстве `PaymentEditDto.PaymentId`
  игнорируется — `dto.PaymentId` был всегда `0`, и `_paymentService.Get(0)`
  падал в `NullReferenceException` (500). Показывалось это только сейчас,
  т.к. раньше сохранение категории/описания на этой странице, видимо, никто
  не тестировал сквозным ручным прогоном. Фикс — добавить `paymentId` в
  тело запроса (как уже делает главная страница).

## Тестовая инфраструктура backend (задача 0003, 2026-10-20)

- xUnit-проекты `FinCtrl/tests/FinCtrl.Domain.Tests` и `FinCtrl/tests/FinCtrl.Application.Tests` (NSubstitute, `FakeTimeProvider`), входят в `FinCtrl.Backend.sln`; запуск — `dotnet test FinCtrl.Backend.sln`.
- `InternalsVisibleTo("FinCtrl.Application.Tests")` в Application — тесты создают `internal`-сервисы напрямую.
- Текущее время: `TimeProvider.System` регистрируется в `AddApplication()`; сервисы Application и `AccountsApiController`/`TagsApiController` берут время из него. Фабрики Domain (`CreateNew`, `FromExcelPayment`) и `Updated()` принимают время параметром — `DateTime.Now` в Domain/Application больше нет. `CreatedAt` категории ставит `CategoryService.Create` (раньше — репозиторий).
- Попутно (чистка предупреждений в изменённых файлах): `PaymentSourceService.MapCategories` теперь ожидается (`await`) и без `.Result`; `ICategoryService.GetById` возвращает `Category?`.

## Тестовая инфраструктура frontend (задача 0004, 2026-10-20)

- Vitest 3 + `@vue/test-utils` + `jsdom`; конфиг `test` в `vite.config.js` (`defineConfig` из `vitest/config`), скрипт `npm run test` (`vitest run`).
- Тесты — `*.spec.js` рядом с кодом; первые — `src/utils/heatmap.spec.js` (палитра, прозрачность и классы тепловой карты, максимум по дереву, формат суммы со знаком).

## Заголовки периодов на главной (задача 0007, 2026-10-20)

- `src/utils/periodHeader.js`: `formatPeriodHeader(period, groupingType)` строит заголовок по `{ from, to }` периода (`to` — исключающая граница): Месяц «Окт 2025», Неделя «13.10 – 19.10.2025», Квартал «IV кв. 2025» (с задачи 0005), Год «2025», День «15.10.2025». Константы `GroupingType` совпадают с enum backend.
- `PaymentsTable.vue` фиксирует тип группировки на момент запроса и хранит готовый заголовок в `periodHeaders[].label`.

## Группировка «Квартал» на главной (задача 0005, 2026-10-20)

- Раньше `GroupingType.Quartal` (= 3) не обрабатывался на backend → `NotImplementedException` и 500. Теперь `MainStatisticService` группирует по началу квартала (1-е число месяцев 1/4/7/10), конец периода — исключающая граница `+3 месяца` (как у месяца/года); `PaymentsApiController.ToPeriodKey` даёт ключ `Q<N><yyyy>` (например `Q42025`).
- Тесты: `FinCtrl/tests/FinCtrl.Application.Tests/Statistic/MainStatisticServiceTests.cs`, `periodHeader.spec.js`.
- **Грабля (не исправлена, вне объёма 0005):** `PaymentRepository.GetTagsForPayments` передаёт все id платежей одним `IN @Ids` — при периоде больше ~2100 платежей SQL Server отвечает «too many parameters», и `GET api/PaymentsApi` падает с 500 при любой группировке (на dev-базе — период с 01.01.2025).

## Одиночное сохранение источника платежа (задача 0006, 2026-10-20)

- Кнопка 💾 на странице «Источники» шлёт `PUT api/PaymentSourcesApi/{id}` (без id в теле), а маршрута с id не было → 404. Добавлен action `EditSource` с `[HttpPut("{paymentSourceId:int}")]` и телом `EditPaymentSourceRequest` (`paymentSourceName` обязателен, `categoryId == null` — снять категорию), ответ `204`. Старый `PUT api/PaymentSourcesApi` (id в теле, массовое сохранение) сохранён.
- Логика правки (поиск категории + `PaymentSource.CreateFromExisting` + `Update`) перенесена из контроллера в `IPaymentSourceService.Edit`; оба PUT используют её. Тесты — `FinCtrl/tests/FinCtrl.Application.Tests/PaymentSources/PaymentSourceServiceTests.cs`.
- В отличие от `PaymentEditDto`, у источника `categoryId == null` **очищает** категорию (так было и раньше в массовом PUT).

## Удалённые платежи в модалке источника (задача 0008, 2026-10-20)

- `PaymentRepository.GetBySource` (`GET api/PaymentsApi/by-source/{id}`, модалка «Показать платежи» на странице «Источники») выбирал платежи без фильтра `IsDeleted = 0` и показывал мягко удалённые. Добавлено условие `AND p.IsDeleted = 0`.
- **Грабля:** удаление платежей мягкое (`IsDeleted = 1`), фильтр в SQL пишется в каждом запросе вручную — в новых выборках из `Payments` не забывать `p.IsDeleted = 0`. Единственный запрос без фильтра (`PaymentRepository.GetAll`, нужный только legacy MVC) удалён задачей 0011.

## Категории источников одним запросом (задача 0009, 2026-10-20)

- `PaymentSourceService` больше не догружает категории источников через `ICategoryService` (`MapCategories` удалён, вместе с N+1 и тихой подменой: при ненайденной категории источник отдавался с «пустой» категорией с одним id). Зависимость `ICategoryService` из сервиса убрана; `Edit` собирает `new Category(categoryId)` — для UPDATE нужен только id.
- `PaymentSourceRepository`: общий `SelectPaymentSource` с `LEFT JOIN Categories` для `GetAll`/`GetById`/`GetByName`/`GetListPaged`, маппинг через плоскую строку `PaymentSourceRow` (алиасы `Category_*`) → `MapToPaymentSource()`; методы чтения переведены на `QueryAsync`.
- Поведение: `PUT api/PaymentSourcesApi[/{id}]` с несуществующим `categoryId` раньше молча снимал категорию, теперь падает на FK `FK_PaymentSources_Categories_CategoryId` (500); нормальный 400/404 — задача 0017.

## Удаление мёртвых backend-проектов (задача 0010, 2026-10-20)

- Удалены заброшенные проекты EF Core-переписывания (net6, вне `FinCtrl.Backend.sln`): `FinCtrl/FinCtrl.Backend.Core.RestAPI/`, `FinCtrl/FinCtrl.DAL/`, `FinCtrl/src/Payments/*` (4 проекта), `FinCtrl/src/FinCtrl.CoreLib/`. Рабочий код на них не ссылался, поведение приложения не изменилось.
- Весь backend теперь — проекты `.sln`: `FinCtrl.Website`, `src/main/*`, `src/common/*`, `tests/*`.

## Удаление legacy MVC-фронтенда (задача 0011, 2026-10-20)

- Из `FinCtrl.Website` удалены MVC-контроллеры (`MainPageController`, `PaymentController`, `PaymentSourceController`, `CategoryController`), `Views/`, `Models/`, `wwwroot/` (~14 МБ вендорных библиотек), `jsconfig.json`, пакет `Microsoft.VisualStudio.Web.CodeGeneration.Design`. `Startup`: `AddControllers` + `MapControllers`, без `UseStaticFiles` (Swagger UI отдаёт статику сам).
- Больше нет пишущих endpoint’ов в обход SPA без antiforgery (`/Payment/delete|edit|editPayments|upload`, `/Category/update`, `POST /payment-source`). `GET /` на backend → 404; все `api/*` и `/swagger` без изменений.
- Удалён код, нужный только MVC: `IPaymentService`/`IPaymentRepository.GetAll(Pagination)` и `.Delete` (мягкое удаление платежа — в SPA удаления нет; понадобится — добавить вместе с API), класс `Pagination`, `DateTimeExtensions.TryParseExact`, `IPaymentSourceService.Update(params)` (встроен в `Edit`), `CategoryDto.ToModel`/`IsOnlyIncomeStr`/`ParentCategoryId`/`OrderIndex` — из ответов `api/PaymentsApi` у `category`/`effectiveCategory` пропали всегда пустые `parentCategoryId`/`orderIndex`.

## Чистка мусора в backend-коде (задача 0012, 2026-10-20)

- Удалены черновики и мёртвый код: `struct Test` с `Main` и `CsvMap` (`CsvFileFormat`), `catch { throw; }` в `XlsxFileFormat`, `Domain/CategoryTree.cs`, `DateRange.Intersect`, неиспользуемая переменная в `MainStatisticService`, `Program.OnConfiguring`, закомментированный код (Program, Startup, PaymentsImportService, PaymentSourceRepository, GetPaymentsTableResponse, launchSettings).
- Удалены неиспользуемые члены: `IPaymentService`/`IPaymentRepository.Exists(sum, date)` (дубли ищет `FindDuplicate`), `IPaymentSourceService`/`IPaymentSourceRepository.Delete`, `ITagService.GetByName` (у репозитория остался — нужен `Exists`), `PeriodDto.ChildrenSum` (из ответа `GET api/PaymentsApi` пропало всегда нулевое поле `childrenSum`, фронтенд его не читал).
- `ExcelPackage.LicenseContext` выставляется только в `XlsxFileFormat` (раньше ещё и в `PaymentsImportService`).
- Папка/namespace `Controllers/PaymentSourceesApi` → `PaymentSourcesApi` (URL `api/PaymentSourcesApi` не изменился); у `PaymentsApiController` убран `partial`; лишние `using` удалены во всех проектах (`dotnet format style --diagnostics IDE0005`).
- Удалены пакеты CsvHelper (Website, Application), `Microsoft.AspNetCore.OpenApi`, явный `System.Text.Json`, `System.Linq.Async` и локальный инструмент `dotnet-ef` (`FinCtrl.Website/.config/dotnet-tools.json`).
- `Startup`: CORS-origins читаются из `Cors:AllowedOrigins` (`appsettings.json`), Swagger называется «FinCtrl API», логирование — только NLog (без `AddConsole`/`AddDebug` перед `ClearProviders`).
- Тесты парсеров импорта: `FinCtrl/tests/FinCtrl.Application.Tests/PaymentsImport/FileFormats/` (с задачи 0014 — `FinCtrl/tests/FinCtrl.Infrastructure.Tests/PaymentsImport/`).
- **Грабля:** `CsvFileFormat` пропускает заголовок по `parser.LineNumber == 2`; у файла из одной строки-заголовка без перевода строки заголовок не пропускается и импорт падает на парсинге даты.

## net9 и Microsoft.Data.SqlClient (задача 0013, 2026-10-20)

- Все проекты .sln (включая тестовые) — 
et9.0. System.Data.SqlClient заменён на Microsoft.Data.SqlClient 6.1.1 (IDbConnectionFactory.GetConnectionAsync() возвращает Microsoft.Data.SqlClient.SqlConnection); пакет и Dapper подключены только в FinCtrl.Common.Infrastructure, Infrastructure получает их транзитивно.
- Версии выровнены: Microsoft.Extensions.* — 9.0.4, Microsoft.Extensions.TimeProvider.Testing — 9.0.0; удалены Microsoft.Bcl.TimeProvider (TimeProvider встроен в BCL) и лишний Microsoft.Extensions.DependencyInjection в Infrastructure.
- В FinCtrl.Website включены Nullable и ImplicitUsings; сборка backend — без предупреждений. Чтобы не менять контракт API, в AddControllers выставлено SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true: non-nullable поля DTO **не** становятся неявно обязательными, обязательность — только явным [Required].
- Поведение: PUT api/PaymentsApi/{id} для несуществующего платежа отдаёт 404 (раньше NRE → 500); у платежа без источника правка категории больше не падает с NRE.
- **Грабля:** Microsoft.Data.SqlClient по умолчанию шифрует соединение (Encrypt=True) — строке подключения к локальному серверу нужен TrustServerCertificate=True (в dev-строке есть).
- **Грабля (окружение):** на dev-машине при нехватке памяти SQL Server параллельные запросы SPA ждут memory grant (RESOURCE_SEMAPHORE) до ~25 с и браузер их обрывает (499) — страницы висят на «Загрузка...». Это не код и не драйвер (воспроизводится с MARS и без); помогает освободить память или задать SQL Server min server memory.

## Направление зависимостей между слоями (задача 0014, 2026-10-20)

- `FinCtrl.Application` больше не ссылается на `FinCtrl.Infrastructure`; наоборот, Infrastructure ссылается на Application. Website ссылается на Infrastructure только ради `AddInfrastructure()` в `Startup`.
- Интерфейсы репозиториев (`IAccountRepository`, `ICategoryRepository`, `IPaymentRepository`, `IPaymentSourceRepository`, `ITagRepository`) перенесены в `Application/<Фича>/` (namespace `FinCtrl.Application.<Фича>`), получили XML-doc; сигнатуры не менялись. `PaymentDto`/`PaymentSourceRow` в контракт не входят и остались `internal` в Infrastructure.
- Импорт: `XlsxFileFormat`, `CsvFileFormat`, `FileFormatFactory` (теперь `internal`) — в `Infrastructure/PaymentsImport/`, пакет EPPlus — в Infrastructure. В Application — `IFileFormat` и новый `IFileFormatFactory` (`PaymentsImportService` зависит от интерфейса, регистрация — в `AddInfrastructure()`).
- Новый тестовый проект `FinCtrl/tests/FinCtrl.Infrastructure.Tests` (тесты парсеров и фабрики форматов, `InternalsVisibleTo` в Infrastructure); тесты `PaymentsImportService` с моками — `FinCtrl/tests/FinCtrl.Application.Tests/PaymentsImport/`.
- **Грабля (не исправлена):** `CsvFileFormat` парсит сумму через `decimal.Parse` в текущей культуре — на машине с ru-RU сумма с точкой (`-55.5`) падает с `FormatException` (500), нужна запятая.
