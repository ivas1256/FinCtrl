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

### Backend: что активно, а что мёртвый код

Внутри `FinCtrl/` несколько параллельных структур — важно не путать:

**Активный стек** (используется, работает, здесь вносить изменения):
- `FinCtrl.Website` — единственное исполняемое приложение в решении
  (`FinCtrl.Backend.sln`). ASP.NET Core, там же REST API
  контроллеры и остатки старых MVC-контроллеров/Razor-views (например
  `Controllers/PaymentSourceController.cs` + `Views/PaymentSource/`) —
  это legacy, реально не используется, вместо него работает Vue SPA.
  Program/Startup регистрирует `AddApplication()` + `AddInfrastructure()`,
  CORS настроен на Vue dev-сервер (`localhost:3000/5173/8080/5174`).
- `src/main/FinCtrl.Domain` — сущности (без EF, POCO; `Payment` пока `record`, что нарушает гайд, — см. `architecture-audit.md`).
- `src/main/FinCtrl.Application` — сервисы (бизнес-логика).
- `src/main/FinCtrl.Infrastructure` — репозитории (Dapper, raw SQL).
- `src/common/FinCtrl.Common`, `FinCtrl.Common.Infrastructure` —
  общий `RepositoryBase`, `IDbConnectionFactory`, `Pagination`, `DateRange`.

**Мёртвый код — не трогать, не путать с активным:**
`FinCtrl.Backend.Core.RestAPI`, `FinCtrl.DAL`, `src/Payments/*`,
`src/FinCtrl.CoreLib`. Это заброшенная попытка переписать всё на EF Core
("clean architecture" rewrite), не входит в `.sln`, последний коммит —
2024-05-27, вытеснена текущим Dapper-стеком.

**Доступ к данным:** без ORM, чистый Dapper. Все запросы — raw SQL
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
`FileFormatFactory` выбирает `XlsxFileFormat` (EPPlus) или
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
- **PaymentSourcesApi** — `GET`, `GET list`, `PUT` (только правка,
  create/delete нет).
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
- Легаси Razor/MVC-страницы в `FinCtrl.Website` не дублировались новым
  функционалом (например, у "Счетов" нет legacy-страницы) — если она
  понадобится, это отдельная задача.
- При добавлении новых полей к `Payment` — не забывать `PaymentDto`
  (Infrastructure и Website), `MainStatisticPayment` (используется
  деревом статистики на главной) и все SQL-выборки в `PaymentRepository`.

## Фича "Теги" (добавлено 2026-09-17)

Первая many-to-many связь в проекте (`Tags` + `PaymentTags`,
`db/migrations/0003_tags.sql`), без каскадного удаления на уровне БД
(только `NO ACTION`) — `TagRepository.Delete` сначала вручную чистит
строки `PaymentTags`, затем сам тег.

- Backend: `FinCtrl.Domain.Tag`, `ITagRepository`/`TagRepository`
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
