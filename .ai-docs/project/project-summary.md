# FinCtrl — саммари проекта (актуально на 2026-10-20)

Личное приложение для учёта финансов. Основной сценарий: пользователь
выгружает из банка Excel/CSV со списком операций, приложение их импортирует,
раскладывает по источникам платежа / счетам / категориям и показывает
сводные таблицы.

Документ собран по итогам исследования кодовой базы и доработок (фича
"Счета"). Можно прикладывать целиком к новым запросам как контекст о
проекте — актуализировать по мере изменений.

## Структура репозитория

Один git-репозиторий в корне (`root.FinCtrl`, базовая ветка `main`), три части:

- `FinCtrl/` — backend, .NET 9 + MS SQL Server.
- `FinCtrl.Frontend/FinCtrl.Frontend/` — фронтенд, Vue 3 + Vite.
- `FinCtrl.AiAssistant/` — Python-сервис (FastAPI) классификации источников платежей через LLM, см. `FinCtrl.AiAssistant/README.md`.

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
  CreatedAt, LastUpdatedAt, Description, Classification (NVARCHAR(200) NULL),
  IsAiClassified (BIT, default 0; миграция 0004).
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
  - **Сравнение с прошлым периодом в `GET` (с задачи 0026):** в корне ответа `comparison`
    (`asOf` = D = min(сегодня, «по»), `current`/`previous` — `{key, from, to}`, `currentPeriodTo`,
    `isCurrentPartial`), в каждом узле `comparison: {current, previous}` — знаковые суммы за Т
    (период группировки с D, с его начала по D) и П (предыдущий целиком; может лежать вне фильтра).
    Т без платежей, пересекающийся с фильтром, всё равно есть в `periods`.
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

- `src/config.js` → `APP_CONFIG.apiBaseUrl` из `VITE_API_BASE_URL` (по умолчанию `http://localhost:5000`; с задачи 0021, раньше — захардкоженный корневой `config.js`).
- `src/utils/format.js` — общие форматтеры сумм и дат (с задачи 0021).
- `src/components/AlertMessage.vue` + `src/composables/useAlerts.js` — сообщения об ошибке/успехе вместо `alert()` (с задачи 0022, стайлгайд 4.12).
- Глобальные стили — Bootstrap 5.3 + `src/assets/main.css` (только `--bs-body-font-size: 15px`); тема только светлая (с задачи 0022).
- `index.js` — роуты: `/` (главная), `/dashboard`, `/payments`
  ("Операции"), `/payment-sources`, `/categories`, `/accounts`, `/tags`.
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
  - `tags/Tags.vue` — CRUD тегов (по образцу `Accounts.vue`).
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
  (без учёта потомков; пустой список — все операции периода, включая без
  категории и исключённые из статистики, с задачи 0034), назначение **аддитивное** (не стирает уже
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

## Чистка шаблона Vite во frontend (задача 0019, 2026-10-20)

- Удалены остатки шаблона Vite: `src/components/HelloWorld.vue`, `TheWelcome.vue`, `WelcomeItem.vue`, `src/components/icons/*`, `src/assets/logo.svg`, а также неиспользуемые `src/pages/paymentsTable/Style.css` и `src/utils/sortUtils.js`. В `src/components/` остались только рабочие `CategoryTreeSelect.vue` и `TagMultiSelect.vue`.
- `src/App.vue` — только шаблон (каркас + меню), без `<script>`: импорты шаблонных компонентов и `PaymentsTable` были мёртвыми.
- `PaymentsTable.vue`: убраны отладочные `console.log` и debug-`watch(formData)`. В `src/` не осталось `console.log` (только `console.error` в обработчиках ошибок).
- `README.md` frontend — краткое описание и команды со ссылкой на `CLAUDE.md`; `index.html` — `lang="ru"`.
- Шаблонный `src/assets/base.css` (тёмная тема через `prefers-color-scheme`) пока оставлен — решение в задаче 0022 (удалён).

## Один Bootstrap 5.3 из npm (задача 0020, 2026-10-20)

- Из `index.html` убраны CDN-подключения Bootstrap 5.0.2 (CSS и `bootstrap.bundle.min.js`); Bootstrap — только npm-пакет `bootstrap@^5.3.8`. CSS импортируется в `src/main.js` первым (`import 'bootstrap/dist/css/bootstrap.min.css'`), до `assets/main.css` и стилей `vue-multiselect` — собственные стили по-прежнему переопределяют Bootstrap.
- JS Bootstrap — только через `import { Modal } from 'bootstrap'` (модуль ESM регистрирует и data-API, в т.ч. `data-bs-dismiss="modal"`); глобального `window.bootstrap` больше нет. `Payments.vue`/`PaymentsTable.vue` переведены с `new bootstrap.Modal(...)`/`getInstance` на `Modal.getOrCreateInstance(el)`.
- Страж от регрессии — `src/bootstrapSetup.spec.js` (нет Bootstrap в `index.html`, порядок импорта CSS, нет `bootstrap.*` в `.vue`).
- Визуальные отличия 5.0 → 5.3 (сверено поэлементно на всех 7 страницах): геометрия (положение/размеры элементов) не изменилась; изменились только токены оформления — скругления (4px → 6px, `-sm` 3.2px → 4px), цвет рамок (`rgba(0,0,0,.125)`/`#ced4da` → `#dee2e6`), `text-muted` (`#6c757d` → `rgba(33,37,41,.75)`), текст внутри `.card`/`.table` теперь `--bs-body-color` (`#212529`) вместо унаследованного из `base.css` `#2c3e50` (в тёмной схеме `base.css` заголовки карточек стали читаемыми).
- **Грабля:** в dev-режиме CSS инжектится скриптом после первой отрисовки, а `base.css` задаёт `transition` цвета/фона у `body` — в фоновой (скрытой) вкладке вычисленный цвет `body` может временно быть чёрным/прозрачным. Это не регрессия; в production CSS подключается `<link>`. (С задачи 0022 `base.css` удалён, грабля неактуальна.)

## Общие форматтеры и адрес API из env (задача 0021, 2026-10-20)

- `src/utils/format.js`: `formatCurrency` (рубли с копейками, пустое = 0), `formatSignedCurrency` (со знаком, для тепловой карты; заменил `heatmap.formatHeatCurrency`), `formatShortAmount` (`12к`, `1.5М` — оси графика), `parseDate`, `formatDate` (`07.09.2026`), `formatShortDate` (`07.09`), `formatLongDate` (с днём недели), `formatDateTime` (`07.09.2026 14:05`), `toInputDate` (`YYYY-MM-DD` по локальному календарю). У форматтеров дат второй параметр — заглушка для пустого значения (по умолчанию `-`).
- Удалены локальные копии в `Payments.vue`, `PaymentsTable.vue`, `PaymentList.vue`, `PaymentSourcesList.vue`, `PaymentSources.vue`, `Dashboard.vue`, `Accounts.vue`, `Tags.vue`, `heatmap.js`. Видимые изменения: в модалке платежей источника сумма теперь в общем формате (`-1 795,00 ₽` вместо `-1 795 ₽`).
- **Грабля (исправлено):** дата для input через `new Date(s).toISOString()` считается в UTC, при положительном смещении пояса (UTC+5) локальная полночь даёт предыдущий день — в модалке правки платежа на главной дата была на день раньше, диапазон по умолчанию на дашборде мог сдвинуться ночью. Также `new Date("YYYY-MM-DD")` разбирается как UTC — `parseDate` разбирает такие строки как локальную полночь.
- Адрес API: корневой `config.js` перенесён в `src/config.js`, `apiBaseUrl = resolveApiBaseUrl(import.meta.env)` — `VITE_API_BASE_URL` (завершающий `/` отбрасывается) или `http://localhost:5000`. Локальные переопределения — в `.env.local` (`*.local` и `.env` в `.gitignore`). Пример `.env.example` не добавлен: хук pre-pr-check блокирует любые `.env.*` в диффе (см. «Вне объёма» задачи 0021).
- Тесты: `src/utils/format.spec.js` (в поясе `Asia/Yekaterinburg`, чтобы UTC-сдвиг воспроизводился на любой машине), `src/config.spec.js`, страж `src/sharedFormat.spec.js` (в `.vue` нет локальных `formatCurrency`/`formatDate`, `Intl.NumberFormat`, `toLocale*`, `toISOString().split`).

## Frontend по стайлгайду (задача 0022, 2026-10-20)

- Все компоненты — `<script setup>`: `PaymentCategoryRow.vue` (рекурсивный, имя через `defineOptions`), `PaymentList.vue`, `PaymentSourcesList.vue` переведены с Options API без изменения поведения.
- `alert()` убран из `src/` (31 вызов): ошибки загрузки/сохранения/валидации и сообщения об успехе массовых сохранений — `AlertMessage.vue` + `useAlerts()` над содержимым страницы; ошибки в модалках (правка платежа на главной, категории, массовые теги, платежи источника) — внутри модалки; ошибка создания тега — под полем `TagMultiSelect`. Загрузка данных и начало сохранения сбрасывают прошлые сообщения. `confirm()` (удаление, смена страницы с несохранёнными изменениями) оставлены.
- Главная: спиннер вместо таблицы на время загрузки категорий/платежей и пустое состояние «Нет данных»; модалка правки — `modal-lg`. «Операции»: `modal-lg` у массового назначения тегов, длинные названия в фильтре источников переносятся (раньше — горизонтальная прокрутка списка), кнопки шапки переносятся на узком экране.
- Удалён шаблонный `src/assets/base.css` (create-vue): его тёмная тема через `prefers-color-scheme` красила только `body` — в тёмной теме ОС заголовок страницы на чёрном фоне и текст в белых карточках был почти не виден; `transition` цвета у `body` давал мигание в dev. Теперь шрифт/цвет/межстрочный — Bootstrap (`system-ui`, `#212529`, 1.5), размер текста оставлен 15px (`main.css`). Приложение всегда светлое; dark-правило тепловой карты в `PaymentCategoryRow.vue` убрано.
- Исправлено попутно: в `PaymentList.vue` `payment.paymentSource.description` без `?.` ронял список платежей ячейки при платеже без источника; в разделе «Frontend» добавлены `/tags`, `Tags.vue`.
- Тесты: страж `src/styleguide.spec.js` (нет Options API, `alert(`, `prefers-color-scheme`/`base.css`; `modal-lg` у модалок правки; спиннер на главной), `useAlerts.spec.js`, `AlertMessage.spec.js`, `TagMultiSelect.spec.js`, компоненты главной `PaymentCategoryRow`/`PaymentList`/`PaymentSourcesList.spec.js`.

## Юнит-тесты на текущий функционал (задача 0023, 2026-10-20)

Characterization-тесты на места, не покрытые профильными задачами; код приложения не менялся. Backend — 169 → 304 теста, frontend — 88 → 110.
- Новый тестовый проект `FinCtrl/tests/FinCtrl.Common.Tests` (в `.sln`): `DateRange` (`FromInclusiveDates`, равенство, `CompareTo`), `DateTimeExtensions`.
- Domain: фабрики `Account`/`Tag`/`PaymentSource`, `Updated()`, `Payment.FromExcelPayment` (сумма ≤ 0 → `Spending`), конструктор `Payment`, равенство категорий по id, фейковые категории.
- Application: `CategoryService.GetAllChildCategories` (обход в ширину, сама категория не входит); `MainStatisticService.Calculate` — неделя с понедельника, день, год, категория источника, итог «ВСЕГО» без доходных/«Накоплений»/«Без категории», порядок по `OrderIndex`, проброс `tagIds`; `DashboardService` — график по месяцам (подписи `MMM yy` ru-RU), только траты, 1-е число месяца/воскресенье в сводке, `TopPayments`, сортировка категорий; `PaymentService.BulkAssignTags` по итоговой категории; импорт — закрытие потока, строка без счёта, тип по знаку суммы, «Списание» с положительной суммой сохраняется как есть.
- Infrastructure: маппинги строк выборки `PaymentDto.MapToPayment` (в т.ч. `NormalizeSumSign`), `PaymentSourceRow`, `PaymentTagRow`; CSV — тип и счёт не заполняются, пустые строки и BOM; XLSX — строки после пустой даты не читаются, пустые ячейки → `null`, читается только первый лист.
- Website (`FinCtrl.Website.Tests`, добавлены NSubstitute и `FakeTimeProvider`): `ApiExceptionHandler` (400/404/409/500, 499 при отмене клиентом, запись JSON при отказе писателя ProblemDetails); контроллеры — даты «по включительно» (`PaymentsApi.Get`/`GetList`/`BulkAssignTags`, `DashboardApi.GetChart`), id из роута/тела, создание счёта/тега; DTO `DashboardSummaryDto`, `PaymentSourceDto`.
- Frontend: `CategoryTreeSelect.spec.js`, `pages/categories/CategoryNode.spec.js`, граничные случаи `heatmap.spec.js`/`periodHeader.spec.js`.
- **Грабли, зафиксированные тестами (не исправлены, см. «Вне объёма» в `plan.md` 0023):** строка XLSX с пустой ячейкой источника роняет импорт `ArgumentNullException` (500) вместо 400; пустое имя источника (CSV) создаёт источник с пустым именем; `heatClass(v, 0)` для расхода даёт `hm-hi`.

## Главная: цветовые токены и панель фильтров (задача 0024, 2026-10-23)
Только frontend. Цвета главной вынесены в CSS-переменные `--fc-*` (`src/assets/main.css`, светлая тема); `PaymentsTable.vue`/`PaymentCategoryRow.vue` ссылаются на них, литералов цвета в scoped-стилях нет (кроме тепловой шкалы; страж — `styleguide.spec.js`).
- Текст `th` — `#5c636a` + `scope="col"`, «Итого» — `#0a58ca`, легенда-градиент 0.04→0.36 (как `heatAlpha`), карточка таблицы `mb-4`, «Без категории» — серое имя веса 400 (`UNCATEGORIZED_ID` в `heatmap.js`), промежутки в ячейке категории только `gap`.
- Панель фильтров: `TagMultiSelect` подогнан под `sm` (31px, рамка `--fc-border`, радиус 4px, 14px), «Отправить» справа (`ms-auto`), у быстрых периодов `aria-pressed`, ручная смена даты снимает активный быстрый период.
- Токены `--fc-cell-text*` сохраняли прежние цвета сумм в ячейках — удалены задачей 0029.
- Документы: `payments-table-heatmap.md` приведён к коду (формула тепла, легенда, точки, размеры), в §11 добавлено исключение для зелёного «снижения трат» (В5).

## Сравнение с прошлым периодом: суммы в API таблицы (задача 0026, 2026-10-23)
Только backend; гибридная схема из `payments-table-month-comparison-requirements.md` §5.1: границы и суммы — бэкенд, Δ — фронтенд (задачи 0027–0028).
- `MainStatisticService.GetPaymentsTable` считает `PaymentsTable.Comparison` (D, Т по D включительно, П целиком, конец полного Т; `IsCurrentPartial`) и `PaymentsTableNode.Comparison` (`Current`/`Previous`) для любой группировки. «Сегодня» — через `TimeProvider`.
- Суммы сравнения — **вторая выборка** `Calculate` за `[начало П, D + 1)` с теми же тегами (П часто вне фильтра); по узлу — свои + потомки, как ячейки; «ВСЕГО» не включает доходные, «Накопления», «Без категории».
- Фильтр дат влияет на сравнение только через D (СР-О13): Т — с начала периода, даже если «с» внутри Т, поэтому сумма Т может отличаться от ячейки колонки Т.
- Т без платежей добавляется в `periods`, если пересекается с фильтром (пустые ячейки).
- Ключи периодов в JSON-объекте `periods` вида `102026` — целочисленные, поэтому в JS-объекте они идут раньше ключей с ведущим нулём (`092026`); порядок колонок строить по `from`, а не по порядку ключей.

## Сравнение с прошлым периодом: расчёт Δ и форматтеры (задача 0027, 2026-10-23)
Только frontend, чистые функции без UI (колонка — задача 0028).
- `src/utils/periodComparison.js`: `getSemantics(node, inSavings)` (расход/доход/«Накопления» — id 33, потомки наследуют), `magnitude` (доход и «Накопления» `+v`, траты `−v`; знак «Накоплений» уточнён в 0028), `isIncomeSemantics` (цвет: у доходов и «Накоплений» рост — хорошо), `compare(sПрев, sТек)` → `{ state, direction, deltaRub, pct }` (`pct`: целое со знаком Δ₽ | `'new'` | `'small'` (<1 %) | `null`), `compareNode`. Знаменатель процента — `|S_П|`.
- `src/utils/format.js`: `formatSignedRoundedCurrency`, `formatSignedPercent`, `formatDelta` (минус U+2212, NBSP перед «₽» и «%», округление половины от нуля — `roundHalfAwayFromZero`).
- `src/utils/periodHeader.js`: `formatComparisonHeader` (`{ text, title }`), `formatComparisonCaption`, `formatComparisonTitle` по таблице СР §2.1 для 5 группировок; год добавляется, когда Т и П в разных годах.

## Главная: колонка «Δ к прошлому периоду» (задача 0028, 2026-10-23)
Только frontend. Последняя колонка тепловой карты — изменение к прошлому периоду группировки (СР §2).
- Колонка показывается, когда в ответе `GET api/PaymentsApi` есть `comparison` и таблица не пустая; группировка и признак «выбраны теги» фиксируются в `submitForm` на момент запроса. Шапка/`title` — `formatComparisonHeader`, подпись под таблицей — `formatComparisonCaption` (+ «, с учётом выбранных тегов») и легенда цветов.
- Ячейка — `src/pages/paymentsTable/DeltaCell.vue` (вью-модель `deltaCellView` в `periodComparison.js`: стрелка ▲/▼/•, тон bad/good/neutral, скрытый текст для скринридера); процентная часть — `formatDeltaPercent` из `format.js`. Цвет: рост трат — `--fc-danger-text`, снижение — `--fc-success-text`; у доходных строк и «Накоплений» (`inSavings` передаётся потомкам в `PaymentCategoryRow`) инвертирован. «Итого» — Δ узла «ВСЕГО».
- Δ-ячейки стали кликабельными в задаче 0031 (карточка сравнения).
- Знак «Накоплений»: на dev-базе месячные суммы положительные (поступления), поэтому `magnitude` для них `+v`, как для дохода (в 0027 было `−v`).

## Главная: ячейки сумм — «—», цвета, клавиатура (задача 0029, 2026-10-23)
Только frontend, `PaymentCategoryRow.vue`.
- Пустая ячейка (сумма 0/нет данных) — «—» цветом `--fc-text-secondary`, `aria-label="нет трат"`, без клика, фокуса, ховера и `title`. Непустая — `role="button"`, `tabindex="0"`, `aria-label` «категория, период: сумма», `aria-pressed`; Enter/Space вызывают клик.
- Текст расходов — `--fc-text` (#212529), поступлений — `--fc-success-text`; класс `hm-hi` и токены `--fc-cell-text*` удалены, `heatClass` для расхода возвращает `''` (и при `maxV = 0`).
- Ховер — рамка (`outline`), фокус и выбранная — `outline` 2px `--fc-accent-text`, выбранная — вес 600, выбранная + фокус — внутренняя тень.

## Главная: карточка сравнения по клику на Δ (задача 0031, 2026-10-23)
Только frontend; данные — те же `comparison` узлов ответа `GET api/PaymentsApi`, без нового запроса (СР §5.1).
- Δ-ячейка с данными — кнопка (`role="button"`, `tabindex="0"`, Enter/Space, `aria-expanded`, `aria-controls="comparison-card"`, `aria-label` — `deltaAriaLabel`, `title` — `formatComparisonCellTitle`), ховер/фокус/выбранная — как у ячеек периода; «—» не интерактивна. `data-delta-key` (categoryId или `total`) — для возврата фокуса.
- `PaymentsTable.vue`: `comparisonScopeKey` (categoryId или `'total'`); под таблицей либо блок платежей, либо карточка (`handleCellClick` закрывает карточку, `toggleComparison` — платежи), повторный клик — закрыть, `submitForm` — закрывает. «Скрыть»/Esc возвращают фокус на Δ-ячейку или (строка свёрнута) на заголовок «Расходы по категориям» (`h5 tabindex="-1"`). Охват ищет `findComparisonScope` (узел, родитель, семантика) — сворачивание родителя карточку не закрывает.
- `ComparisonCard.vue`: заголовок `formatComparisonTitle` · охват (точка + «Родитель / Подкатегория», для «Итого» — «Все траты»), «Скрыть», подпись-период `formatComparisonPeriodNote`, плитки Т/П (`magnitude` по семантике строки, `formatCurrency`) и «Разница» (`deltaCellView`). `reveal()` — фокус на заголовок и `scrollIntoView({ block: 'nearest' })`, без плавности при `prefers-reduced-motion`. Топ-3 и таблица подкатегорий — задачи 0032–0033.
- Новые функции: `periodHeader.js` — `formatComparisonPhrase`, `formatComparisonCellTitle`, `formatComparisonPeriodNote`, `formatComparisonTileLabels`; `periodComparison.js` — `findComparisonScope`, `isDeltaInteractive`, `deltaAriaLabel`; `format.js` — `formatDeltaSpoken`.

## Карточка сравнения: расчёт строк (задача 0032, 2026-10-23)
Только frontend, без UI: чистые функции в `src/utils/comparisonCard.js` (СР §4.4–4.5, §4.7). `buildComparisonRows({ node, semantics })` (охват-категория) или `({ treeRows })` («Итого») → `{ hasSubcategories, rows }`; строка: `{ key, name, level, semantics, isService, sPrev, sCur, view (deltaCellView), children }`. Дети в порядке дерева, внуки рекурсивно, строка «{Имя} (без подкатегории)» при |own| ≥ 0,005; пустые (S_П = S_Т = 0) отбрасываются; семантика дохода/«Накоплений» наследуется. `topChanges(rows)` → `{ growth, decline }` (топ-3), `sortComparisonRows(rows, SortMode)` (`category`/`change`, новый массив), `barScale(rows)` (M по всем строкам) и `barShare(row, M)`. Подключение к карточке — задача 0033.

## Массовые теги всем операциям периода (задача 0034, 2026-10-26)
- `POST api/PaymentsApi/bulk-tags`: пустой `categoryIds` (или поле не передано) — теги получают все платежи периода (`GetByDateRange`), включая без итоговой категории и с `ExcludeFromStats`; `affectedCount` — их число. Раньше — 400 «Выберите хотя бы одну категорию» (`[MinLength(1)]` в `BulkAssignTagsRequest` и проверка в `PaymentService.BulkAssignTags` убраны). Теги и период по-прежнему обязательны.
- «Операции», окно «Назначить теги»: поле «Категории» необязательное — плейсхолдер «Все категории», подсказка «Не выбрано — теги получат все операции периода».

## AiAssistant: API классификации источников платежей (задача 0035, 2026-11-03)
- `FinCtrl.AiAssistant/` из скрипта стал FastAPI-сервисом: `POST /payment-sources/classify` (`list[PaymentSource]` → `list[ClassifiedPaymentSource]`), запуск — `uvicorn main:app --port 8000` (preview-конфигурация `ai-assistant`).
- Ключ/адрес/модель LLM — из env или `.env` (`LLM_API_KEY`, `LLM_BASE_URL`, `LLM_MODEL`), образец — `.env.example`; `.env`, `logs/`, `payment-sources.json` не коммитятся.
- Запрос к LLM — `LlmClient` (`llm_client.py`, ошибки openai → `LlmError`); проверка ответа — `parse_classification` (`response_validation.py`): JSON (массив или объект с одним полем-списком, ```json-ограждение срезается), схема, те же Id без дублей, непустая классификация; `Name` — из запроса.
- Ошибки: 422 — невалидный запрос (WARNING в лог), любая другая — 500 `{"detail": "Внутренняя ошибка сервиса"}` + трейс в `logs/errors.log`. Автотестов нет (решение пользователя).

## Классификация источников через AiAssistant (задача 0036, 2026-11-03)
- БД (миграция `0004`): `PaymentSources.Classification` (`NVARCHAR(200) NULL`) и `IsAiClassified` (`BIT NOT NULL DEFAULT 0`). Поля есть в `SelectPaymentSource`, `SelectPayment` (`PaymentSource_Classification`/`_IsAiClassified`) и в `PaymentSourceDto` (`classification`, `isAiClassified`), т.е. и во вложенном источнике `PaymentDto`.
- Правила (Domain `PaymentSource`): `SetClassificationByUser` — trim, пустая строка = очистить, флаг ИИ снимается **только при реальном изменении** значения (массовое сохранение строки с правкой категории признак не трогает); `SetAiClassification` — ставит флаг. `PaymentSourceService.Edit` теперь правит загруженный источник (а не собранный заново), `PUT api/PaymentSourcesApi[/{id}]` принимает `classification`. `PaymentSource.CreateFromExisting` удалён.
- Автоклассификация: `IPaymentSourceClassificationService` (Application, `PaymentSources/Classification/`) режет новые источники на пачки `AiAssistant:BatchSize` (appsettings, по умолчанию 30), зовёт `IPaymentSourceClassifier` → `AiAssistantClient` (Infrastructure, typed `HttpClient`, `AiAssistant:BaseUrl`/`TimeoutSeconds`, JSON в PascalCase) и сохраняет `UpdateClassifications` с `IsAiClassified = 1`. Ошибки клиента оборачиваются в `PaymentSourceClassificationException`; сервис её не бросает, а возвращает текст — обработанные пачки остаются сохранены.
- Импорт: `IPaymentsImportService.Upload` возвращает `PaymentsImportResult` (созданные платежи и источники, `ClassificationError`); классификация — **после коммита** транзакции импорта, её ошибка импорт не откатывает. `POST api/PaymentsApi/upload` → `200 UploadPaymentsResponse { newPayments: PaymentDto[], newPaymentSources: PaymentSourceDto[], classificationError }` (раньше пустой `200`). Options секции `AiAssistant` привязываются в `Startup`.
- Frontend: кнопка «Загрузить файл» — в меню `App.vue` (на «Операциях» её больше нет), открывает `components/PaymentsImportModal.vue` (`modal-lg`): файл + «Загрузить», спиннер на время импорта и LLM, вкладки-кнопки «Новые платежи (N)» / «Новые источники платежей (N)», предупреждение `classificationError`. `composables/usePaymentsImport.js` — общий счётчик импортов: «Операции» и уже построенная таблица главной перезагружаются после импорта.
- «Источники платежей»: колонка «Классификация» (input, change tracking, значок «ИИ» у неизменённой LLM-классификации). Главная, «Распределение по источникам»: группировка `utils/paymentSourceGroups.js` по классификации (без неё — по имени источника), под названием серым — оригинальные имена.
