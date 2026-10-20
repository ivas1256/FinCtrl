# FinCtrl — саммари проекта (актуально на 2026-10-20)

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
- `src/common/FinCtrl.Common.Application` (с задачи 0017) — исключения приложения
  `NotFoundException`/`ValidationException`/`ConflictException` и `Paging.Validate`.

**Ошибки API (с задачи 0017):** сервисы бросают исключения приложения, глобальный
`ApiExceptionHandler` (`Website/ErrorHandling/`, `AddProblemDetails` + `UseExceptionHandler`)
отдаёт ProblemDetails (`application/problem+json`: `title`, `detail`, `status`):
`ValidationException` → 400, `NotFoundException` → 404, `ConflictException` → 409, прочее → 500
(в Development `detail` = текст исключения, иначе общий текст; стек — в лог). Ошибки DataAnnotations
на DTO — автоматический 400 `ValidationProblemDetails` с полем `errors`. Пустые 4xx (неизвестный маршрут) —
тоже ProblemDetails (`UseStatusCodePages`). Страницы исключения больше нет и в Development.
Frontend показывает текст через `src/utils/apiError.js` → `getErrorMessage(error, fallback)`.

Других backend-проектов в репозитории нет: заброшенная попытка
переписать бэкенд на EF Core ("clean architecture" rewrite вне `.sln`)
удалена задачей 0010.

**Доступ к данным:** без ORM, чистый Dapper поверх Microsoft.Data.SqlClient. Все запросы — raw SQL
внутри репозиториев. `RepositoryBase` даёт только async-хелперы
`Execute/ExecuteScalar/Query/FirstOrDefault` (с задачи 0015 синхронных вызовов Dapper нет). Dommel/FluentMap в коде нет, раньше
документация упоминала их по ошибке.

**Транзакции (с задачи 0016):** соединение одно на scope (`DbConnectionFactory`, scoped), он же реализует
`IUnitOfWork` (`FinCtrl.Common`). Сервис открывает транзакцию `await using var tx = await _unitOfWork.BeginTransaction();`
… `await tx.Commit();` — освобождение без `Commit` откатывает. Все хелперы `RepositoryBase` автоматически идут в текущей
транзакции; соединение наружу не отдаётся (прямой `conn.QueryAsync` без транзакции упал бы). Повторный `BeginTransaction`
при открытой транзакции возвращает вложенную no-op — репозиторий может сам защищать свою многошаговую запись
(`SetPaymentTags`, `AddTagsToPayments`, `TagRepository.Delete`), а сервис — объединять несколько репозиториев (импорт).

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
и для `Account`): получить по имени (`GetByName`/`GetByExcelName` — единственные методы
сервисов, возвращающие `null`; `GetById` с задачи 0017 бросает `NotFoundException`) →
если нет, создать и перечитать по id; результат кэшируется по имени на
время импорта файла (с задачи 0016 — один запрос на имя за файл, а не
два на строку). Это общий шаблон для любой
lookup-сущности, создаваемой автоматически при импорте — держаться его и
для будущих подобных сущностей.

**Атомарность и лог (с задачи 0016):** весь файл импортируется в одной
транзакции — ошибка на любой строке откатывает и платежи, и созданные
источники/счета (identity при этом расходуются). Отмена запроса
(`CancellationToken`) проверяется перед каждой строкой. Итог пишется в
лог: `Импорт файла …: строк N, создано платежей …, дозаполнен счёт …, пропущено дублей …`.

**Ошибки файла (с задачи 0017) → 400:** неподдерживаемое расширение, не-xlsx содержимое,
некорректная дата/сумма или мало колонок (`Строка N: …`), файл без операций (пустой или только
заголовок). CSV разбирается независимо от культуры сервера: дата — `ru-RU` (`01.10.2025 12:30:00`)
или ISO, сумма — с запятой или точкой, пробелы-разделители разрядов игнорируются.

**Дедупликация / дозаполнение:** для каждой строки файла ищется уже
существующий платёж с той же суммой и датой (`FindDuplicate`). Если не
найден — создаётся новый. Если найден и у него **ещё не проставлен**
`Account`, а в текущей строке файла счёт есть — он дозаполняется
(`Update`), без создания дубликата. Так повторная загрузка того же файла
безопасно проставляет счета старым операциям.

## Backend API (ключевые контроллеры, `FinCtrl.Website/Controllers`)

- **PaymentsApi** — `GET` (таблица главной: дерево категорий × периоды, с
  датами и группировкой), `PUT {id}` (правка платежа: категория / счёт /
  описание / `excludeFromStats` / теги, одной транзакцией, ответ 204), `GET list`
  (пагинация/сортировка/фильтры — страница "Операции"), `GET by-source/{id}`,
  `POST bulk-tags`, `POST upload`. Все платежи в ответах — один `PaymentDto`
  (`category` — своя категория, `effectiveCategory` — итоговая, `categorySource`).
  - **Семантика PUT (`EditPaymentRequest` → `PaymentService.Edit`, с задачи 0018)**:
    id — из роута (`paymentId` в теле игнорируется); поле `null`/отсутствует —
    не трогать. `categoryId`: категория, совпадающая с категорией источника, в
    платёж не пишется, очистить свою категорию нельзя (старое ограничение);
    `accountId`: `0` — очистить; `description`: пустая строка — очистить;
    `excludeFromStats`: `bool`; `tagIds`: список (в т.ч. пустой) — заменить
    целиком. Новые поля — по этой же конвенции (null = не трогать).
  - Даты `from`/`to` в запросах (`GET`, `GET list`, `bulk-tags`, `DashboardApi/chart`) —
    дни «с … по …» включительно; внутри — `DateRange` с исключающей `To`.
- **PaymentSourcesApi** — `GET`, `GET list`, `PUT {id}` (правка одного источника, id из роута; с задачи 0006), `PUT` (legacy: id в теле, используется массовым сохранением); только правка,
  create/delete нет.
- **AccountsApi** — полный CRUD: `GET`, `POST`, `PUT {id}`, `DELETE {id}`.
- **CategoriesApi** — `GET getAll`, `GET getTree` (`CategoryDto` с `children`), `POST` (`{ categoryId }`), `PUT {id}`, `DELETE {id}`.
- **DashboardApi** — `GET summary` (траты, доли категорий, топ-3), `GET chart` (`mode`, `from`, `to`).
- PUT/DELETE во всех API отвечают **204** без тела (с задачи 0018); списки с пагинацией — `PagedResponse<T>` (`items`, `totalCount`).
- **Коды ошибок (с задачи 0017):** PUT/DELETE несуществующего id во всех API → 404; дубликат имени
  счёта/тега (создание и переименование) → 409; удаление счёта с операциями и категории с дочерними
  категориями/источниками/операциями → 409 с количеством связей (учитываются и удалённые операции —
  на них тоже ссылается FK); несуществующие категория/счёт/тег/родитель в правке → 400 (раньше 500 на FK);
  родитель категории — она сама или её потомок → 400; цвет не `#RRGGBB` → 400; `page < 1`/`pageSize < 1` → 400;
  `GET api/PaymentsApi` без `periodType` → 400.

**Грабля, на которую уже наступали:** раньше в `PaymentRepository` было два
раздельно написанных SELECT'а платежей (`_selectPaymentInfo` и inline
`dataSql` в `GetListPaged`), и они расходились (колонку `Account` забыли в
`GetListPaged`, алиас `ParentCategoryId` был неверным в другом). С задачи 0015
SELECT один — `PaymentRepository.SelectPayment`, его алиасы обязаны совпадать
со свойствами `PaymentDto` (проверяет `PaymentRepositoryTests`). **При
добавлении нового поля к Payment** — колонка в `SelectPayment`, свойство в
`PaymentDto` (Infrastructure) + `MapToPayment`, а также Website-`PaymentDto` и
его `FromModel` (с задачи 0018 — единственный маппинг платежа наружу;
`MainStatisticPayment` удалён, таблица главной хранит `Payment`).

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
  (Infrastructure и Website, `FromModel`) и `SelectPayment` в `PaymentRepository`.

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
  SQL join в `SelectPayment`, а отдельным батч-запросом `PaymentRepository.GetTagsForPayments` +
  `AttachTags`, вызываемым после основной выборки в `GetByDateRange` (кормит
  главную страницу через дерево статистики) и `GetListPaged` (кормит
  "Операции"). Фильтр по тегам — SQL `EXISTS`-подзапрос в `GetListPaged`
  (там пагинация), и обычный LINQ-фильтр в памяти в `GetByDateRange` (там
  весь период грузится целиком для дерева).
  Оба пути мапятся в Website-`PaymentDto` одним `PaymentDto.FromModel`
  (до задачи 0018 — двумя разными маппингами в `GetList` и `BuildTree`).
- Массовое назначение тегов — `POST api/PaymentsApi/bulk-tags`
  (`PaymentService.BulkAssignTags`): фильтр за период + список категорий
  (без учёта потомков), назначение **аддитивное** (не стирает уже
  проставленные теги).
- `TagIds` в правке платежа (с задачи 0018 — `EditPaymentRequest`): `null` — не трогать теги, список (в т.ч. пустой)
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
  тело запроса (как уже делает главная страница). С задачи 0018 id берётся
  из роута, `paymentId` в теле не нужен.

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
- **Грабля (исправлена задачей 0015 — id грузятся пачками по 2000):** `PaymentRepository.GetTagsForPayments` передаёт все id платежей одним `IN @Ids` — при периоде больше ~2100 платежей SQL Server отвечает «too many parameters», и `GET api/PaymentsApi` падает с 500 при любой группировке (на dev-базе — период с 01.01.2025).

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
- Поведение: `PUT api/PaymentSourcesApi[/{id}]` с несуществующим `categoryId` раньше молча снимал категорию, теперь падает на FK `FK_PaymentSources_Categories_CategoryId` (500); с задачи 0017 — 400 (несуществующий источник — 404).

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
- **Грабля (исправлена задачей 0017):** `CsvFileFormat` пропускал заголовок по `parser.LineNumber == 2`; у файла из одной строки-заголовка без перевода строки заголовок не пропускается и импорт падает на парсинге даты.

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
- **Грабля (исправлена задачей 0017):** `CsvFileFormat` парсил сумму через `decimal.Parse` в текущей культуре — на машине с ru-RU сумма с точкой (`-55.5`) падает с `FormatException` (500), нужна запятая.

## Async-репозитории и единые SELECT'ы (задача 0015, 2026-10-20)

- `RepositoryBase` и все репозитории используют только async Dapper (`ExecuteAsync`, `ExecuteScalarAsync`, `QueryAsync`, `QueryFirstOrDefaultAsync`); хелперы базы — `Execute`, `ExecuteScalar<T>`, `Query<T>`, `FirstOrDefault<T>`.
- Id после вставки во всех репозиториях — `OUTPUT INSERTED.<Id>` + `ExecuteScalarAsync<int>` (убраны `@generated_keys` и отдельный `SELECT SCOPE_IDENTITY()` у платежей).
- Один `const`-SELECT на репозиторий; у платежей — `PaymentRepository.SelectPayment` для `Get/GetByDateRange/GetBySource/FindDuplicate/GetListPaged`. Исправлен алиас: `PaymentSourceCategory_ParentCategoryId` (у категории источника платежа теперь заполнен `parentCategoryId`). Тест `FinCtrl/tests/FinCtrl.Infrastructure.Tests/Payments/PaymentRepositoryTests.cs` сверяет алиасы SELECT'а со свойствами `PaymentDto`.
- Маппинг без `dynamic`: `QueryAsync<Account>`/`<Tag>`/`<Category>` (у категорий `NULL`-цвет заменяется на `Category.DefaultColor`), теги платежей — через `PaymentTagRow`.
- `GetTagsForPayments` грузит теги пачками по 2000 id — нет 500 на периодах с > 2100 платежами.
- Без изменений (задача 0016): `WITH (NOLOCK)` в SELECT'е платежей, рекурсивная загрузка дерева категорий, отсутствие транзакций. Сделано задачей 0016 (см. ниже).

## Транзакции, дерево категорий, лог импорта (задача 0016, 2026-10-20)

- `IUnitOfWork`/`ITransaction` в `FinCtrl.Common`; реализация — `DbConnectionFactory` (транзакция соединения scope, вложенная — no-op), `RepositoryBase` передаёт текущую транзакцию во все вызовы Dapper, поле фабрики в базе `private`. Подробности — в разделе «Доступ к данным».
- В транзакции: `PaymentRepository.SetPaymentTags` (DELETE + INSERT), `AddTagsToPayments`, `TagRepository.Delete` (связи + тег), импорт файла целиком.
- `PaymentsImportService`: `ILogger` с итогом, `CancellationToken` (из `UploadFile`), кэш источников/счетов — на строку остаются `FindDuplicate` + `Create`/`Update`.
- Дерево категорий (`GET api/CategoriesApi/getTree`, главная) — один SELECT + сборка в `CategoryService.GetCategoriesTree` (сортировка по `OrderIndex`, затем `CategoryId`; категории с несуществующим родителем в дерево не попадают). `ICategoryRepository.GetCategoriesTree` удалён. Ответ `getTree` — единицы мс вместо 1–5 с.
- Знак суммы — доменное правило `Payment.NormalizeSumSign()` (расход с положительной суммой → отрицательная), вызывается маппером при чтении. При импорте не применяется: в dev-базе ~1450 расходов с положительной суммой, а поиск дублей сравнивает сумму как есть.
- `WITH (NOLOCK)` убран из `SelectPayment` и COUNT в `GetListPaged`.
- Не сделано: `EditPayment` (Update + SetPaymentTags) не в одной транзакции — оркестрация в контроллере, переносится в `PaymentService.Edit` задачей 0018.

## Обработка ошибок и валидация (задача 0017, 2026-10-20)

- Новый проект `FinCtrl.Common.Application`: `NotFoundException`, `ValidationException`, `ConflictException`, `Paging`. Глобальный `ApiExceptionHandler` → ProblemDetails, подробности — в разделе «Ошибки API».
- Проверки обязательности и уникальности перенесены из `AccountsApi`/`TagsApi` в `AccountService`/`TagService`; `GetById` сервисов бросает `NotFoundException`; `Update`/`Delete` проверяют существование. Проверка связей перед удалением: `IAccountRepository.CountPayments`, `ICategoryRepository.GetUsage`.
- `PaymentService` проверяет существование категории/счёта (`Update`) и тегов (`SetPaymentTags`, `BulkAssignTags`) до записи; `PaymentSourceService.Edit` — источника и категории.
- `MainStatisticService`: `NotSelected`/неизвестная группировка → `ValidationException` (раньше `NotImplementedException` → 500). `FileFormatFactory` — `ValidationException` вместо `NotImplementedException`.
- Исправлено: `CsvFileFormat` зависел от культуры (`-55.5` на ru-RU → 500) и падал на файле из одной строки-заголовка без перевода строки (`LineNumber` = -1 после последней строки — номер строки теперь считается вручную).
- DataAnnotations: `AccountDto`, `TagDto` (`[Required]`, `[StringLength(255)]`), `CategoryCrudDto` (`[Required]`), `BulkAssignTagsDto` (`[MinLength(1)]`), `UploadFile` (`[Required] file`).
- Лог: `ExceptionHandlerMiddleware` заглушён в `nlog.config` (писал ERROR со стеком на каждую 400/404/409); ожидаемые ошибки `ApiExceptionHandler` пишет как INFO без стека, непредвиденные — ERROR со стеком.
- **Грабля:** NLog (`AddNLog`) убирает фильтры `Logging:LogLevel` из appsettings/`AddFilter` — уровни логирования задаются только правилами `nlog.config`.
- **Грабля (исправлена задачей 0018):** `EditPayment` — `Update` и `SetPaymentTags` в разных транзакциях: при несуществующем теге платёж уже сохранён (`LastUpdatedAt`, описание), а ответ — 400.

## Тонкие API-контроллеры и DTO-контракт (задача 0018, 2026-10-20)

- Логика из контроллеров перенесена в Application: таблица главной — `IMainStatisticService.GetPaymentsTable` (read-модель `Domain/Statistic/PaymentsTable`: дерево, ключи периодов, «Без категории»/«ВСЕГО», период по умолчанию через `TimeProvider`); доли категорий, топ-траты (по модулю) и периоды графика по умолчанию — `DashboardService`; правка платежа — `PaymentService.Edit(PaymentEdit)`: проверки категории/счёта/тегов до записи, `Update` + `SetPaymentTags` в одной транзакции `IUnitOfWork`.
- Website: DTO лежат в `Controllers/<Feature>Api/` (папка `RequestDto/` удалена), один маппинг на пару типов (`FromModel`/`ToModel`), наружу только DTO (`CategoriesApi`, `by-source` больше не отдают сущности), `*Response` вместо анонимных типов (`PagedResponse<T>`, `BulkAssignTagsResponse`, `CreateCategoryResponse`), PUT/DELETE → 204.
- Контракт: `excludeFromStats: bool?` вместо `excludeFromStatsStr: "on"`; `description`/`excludeFromStats` = `null` — не трогать (раньше PUT без них затирал описание и сбрасывал флаг; модалка главной не отправляла флаг); id правки платежа — из роута.
- Исправлено попутно: в `GET list` `paymentSource.createdAt` был `0001-01-01` (ручной маппинг) — теперь общий `PaymentSourceDto.FromModel`; в модалке «Показать платежи» источника категория всегда была «Без категории» (`payment.paymentCategory?.Id`) — теперь `effectiveCategory`.
- Даты: `DateRange.To` везде исключающая. `GET api/PaymentsApi` раньше трактовал `to` как исключающую — день `to` (сегодня) не попадал в таблицу главной; теперь включительно, как в остальных API. Период группировки «День» — `[d, d+1)` (раньше `To == From`). `GET list` без дат — без условия по дате в SQL (вместо 1753/9999).
- Domain: `CategoryIds` (`Savings`, `FakeEmpty`, `FakeTotal`) вместо `33`/`int.MaxValue`, `Category.IsFake`, `Category.ColorOrDefault`; `Payment` — `class` вместо `record`.
- Главная: категория в списке платежей ячейки и в модалке правки — `effectiveCategory` (поле `category` теперь своя категория платежа, как в «Операциях»); у источника в списке показывается его описание (раньше не маппилось).
- Новый тестовый проект `FinCtrl.Website.Tests` — маппинг DTO (`PaymentDto`, `CategoryDto`, таблица главной, `EditPaymentRequest`).
