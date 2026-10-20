# Аудит архитектуры на соответствие регламенту (2026-10-20)

Проверка рабочего кода на соответствие `backend-guide.md`, `styleguide.md` и `CLAUDE.md`. Аудит выполнен чтением кода, без сборки и запуска. Номера строк приведены на коммит `378bb66` и со временем могут сместиться.

Пути backend даны относительно `FinCtrl/`, пути frontend — относительно `FinCtrl.Frontend/FinCtrl.Frontend/`.
Серьёзность: **В** — высокая, **С** — средняя, **Н** — низкая.

## Заключение

Ни одному разделу `backend-guide.md` рабочий стек не соответствует полностью. Главные проблемы:

1. **Мёртвый код в репозитории.** Это 4 заброшенных проекта и legacy MVC/Razor-фронтенд внутри `FinCtrl.Website`. MVC не используется, весь UI работает на Vue, но MVC по-прежнему подключён. Поэтому `GET /` отдаёт Razor-страницу, а пишущие endpoint'ы (`DELETE /Payment/delete`, `POST /Payment/upload`, `/Category/update`) доступны в обход SPA. Решение — удалить его целиком и оставить в Website только REST API.
2. **Обратное направление зависимостей.** Application ссылается на Infrastructure, интерфейсы репозиториев лежат в Infrastructure, парсинг файлов — в Application.
3. **Толстые контроллеры.** `PaymentsApiController` и `DashboardApiController` сами строят дерево, считают периоды, хранят состояние в полях и задают правила правки платежа. Наружу отдаются доменные сущности.
4. **Infrastructure не по гайду.** Dapper вызывается синхронно, транзакций нет, SELECT'ы продублированы (два SELECT'а платежей уже разошлись), маппинг через `dynamic`, в дереве категорий N+1.
5. **Нет сквозной обвязки.** Нет тестов, глобального обработчика ошибок с ProblemDetails, валидации, `ILogger`, `TimeProvider` и XML-doc. TargetFramework у проектов разный: net6, net7 и net9.
6. **Frontend.** Остались файлы шаблона Vite, `console.log` и два Bootstrap (5.0.2 с CDN и 5.3 из npm). Форматтеры продублированы, есть `alert()` и Options API.
7. **Найдено 5 функциональных багов** (см. ниже).

Что уже соответствует гайду:
- структура по фичам и namespace, совпадающий с путём;
- `internal` + `AddScoped`, суффикс `*ApiController`;
- параметризованный SQL и whitelist для `ORDER BY`;
- пагинация в SQL у `GetListPaged`;
- теги грузятся батчем;
- `Nullable` включён в Domain/Application/Infrastructure;
- frontend: страницы в `src/pages/<фича>/`, роуты, меню, Dashboard.

## 1. Мёртвый код

### 1.1. Проекты вне `.sln`
| Путь | Файлов | Суть |
|---|---|---|
| `FinCtrl.Backend.Core.RestAPI/` | 11 | net6, ссылается на `FinCtrl.DAL` |
| `FinCtrl.DAL/` | 13 | net6, EF Core 7 |
| `src/Payments/*` (4 проекта) | 10 | net6, EF Core; ссылается на рабочий `FinCtrl.Common`, обратных ссылок нет |
| `src/FinCtrl.CoreLib/` | 1 | пустой csproj |

Рабочий код на эти проекты не ссылается: нет ни `ProjectReference`, ни `using`. Удаление безопасно → задача **0010**.

### 1.2. Legacy MVC/Razor в `FinCtrl.Website`
Удаляется полностью, в Website остаётся только REST API → задача **0011**.

- **Подключение** — `Startup.cs`:
  - `:26` — `AddControllersWithViews`;
  - `:75` — `UseStaticFiles`;
  - `:84-86` — `MapControllerRoute`.
- **Контроллеры:** `MainPageController` (`[Route("")]`, отдаёт `GET /`), `PaymentController`, `PaymentSourceController`, `CategoryController`. Antiforgery нет. `POST /Category/update` сбрасывает цвет категории, `POST /Payment/editPayments` падает с NRE.
- **Views:** `Views/` — 9 файлов, около 1390 строк.
- **Прочее:** `Models/ErrorViewModel.cs`; `wwwroot/` (около 70 файлов в `lib/`, около 14 МБ); `jsconfig.json`; пустые `<Folder>` в csproj.
- **Код, который держится только на MVC:**
  - `IPaymentService/IPaymentRepository.GetAll(Pagination)` — грузит всю таблицу без `IsDeleted` и сортирует в памяти;
  - `IPaymentService.Delete`, `Pagination`;
  - `CategoryDto.ToModel`/`IsOnlyIncomeStr`, `DateTimeExtensions.TryParseExact`;
  - пакет `Microsoft.VisualStudio.Web.CodeGeneration.Design`.
- **Дублирование:** `MainPageController.cs:102-221` повторяет `BuildTree`/`ToPeriodKey`. В `:94-97` закомментированы `File.WriteAllText` с путями пользователя.

### 1.3. Мусор в рабочем коде → задача **0012**
- `Application/PaymentsImport/FileFormats/CsvFileFormat.cs`:
  - `:46-56` — неиспользуемый `CsvMap`;
  - `:58-77` — `struct Test` со `static Main` и `Console.WriteLine`.
- `XlsxFileFormat.cs:38-42` — `catch (Exception ex) { throw; }`; `LicenseContext` выставляется дважды.
- `Domain/CategoryTree.cs` — пустой класс с TODO. `Common/DateRange.cs:20-23` — `Intersect` бросает `NotImplementedException`.
- `Application/Statistic/MainStatisticService.cs:10` — `using static ...JSType`; `:42` — неиспользуемая `var t`.
- Закомментированный код:
  - `Program.cs:19-34` (EF `OnConfiguring`);
  - `Startup.cs:74,77`;
  - `PaymentsImportService.cs:46`;
  - `PaymentSourceRepository.cs:187`;
  - `GetPaymentsTableResponse.cs:11`.
- `Startup.cs:49-56` — `AddConsole`/`AddDebug` вызываются прямо перед `ClearProviders`. Swagger называется «My API».
- Неиспользуемые члены интерфейсов: `IPaymentService/IPaymentRepository.Exists`, `IPaymentSourceService/Repository.Delete`, `ITagService.GetByName`. Также `PeriodDto.ChildrenSum` и `GET api/CategoriesApi/getAll`: этот маршрут фронт не вызывает, но URL оставляем.
- Опечатка в имени папки и namespace: `Controllers/PaymentSourceesApi/`. `partial class PaymentsApiController` без второй части. Лишние `using` почти во всех файлах.
- Лишние пакеты:
  - CsvHelper (Website и Application);
  - `Microsoft.AspNetCore.OpenApi 7.x`;
  - явный `System.Text.Json`;
  - `System.Linq.Async`;
  - `.config/dotnet-tools.json` (`dotnet-ef`).
- `db/migrations/README.md` не упоминает `0003_tags.sql`.

## 2. Нарушения backend по слоям

### 2.1. Зависимости (§2 гайда) → **0014**
- **В:** `Application.csproj` ссылается на Infrastructure. Все 5 `I*Repository` лежат в `Infrastructure/<Фича>/`, а сервисы подключают их через `using FinCtrl.Infrastructure.*`.
- **С:** парсинг Excel/CSV (EPPlus, `TextFieldParser`) лежит в Application, хотя по гайду это задача Infrastructure.
- **Н:** `IDbConnectionFactory` возвращает конкретный `System.Data.SqlClient.SqlConnection` (→ **0013**).

### 2.2. Domain (§3)
- **С:** `Payment` объявлен как `record`, при этом мутабельный и содержит `List<Tag>` → **0018**.
- **С:** магические id. `Category.cs:61` (`!= 33` — «Накопления») и `int.MaxValue` / `int.MaxValue - 1` для фейковых категорий, проверка продублирована в `DashboardService.cs:104` → **0018**.
- **С:** `DateTime.Now` используется в 10 местах Domain и в `CategoryRepository.Create` → **0003** (`TimeProvider`).
- **Н:** у необязательных строк нет `= string.Empty` (CS8618). Комментарий к `Payment.CalculatedCategory` не соответствует коду. `GroupingType.Quartal` не поддерживается (→ **0005**).

### 2.3. Application (§4)
- **В:** `PaymentSourceService.cs:29,42,55` вызывает `async MapCategories` без `await`, а внутри стоит `.Result`. Исключения теряются, на каждую категорию отдельный запрос (N+1) → **0009**.
- **С:** ошибки ввода сигнализируются через `NotImplementedException`: `FileFormatFactory.cs:20`, `MainStatisticService.cs:80,92`, `DashboardService.cs:68`. `NotFoundException`/`ValidationException` не заведены, контракт про null не описан → **0017**.
- **С:** проверка уникальности имени выполняется в контроллерах, а не в `AccountService`/`TagService`. Перед `Delete` не проверяются связи, поэтому FK без каскада даёт 500 → **0017**.
- **С:** импорт работает без транзакции, без логов и без `CancellationToken`, на каждую строку уходит 3–5 запросов → **0016**.
- **С:** у `DateRange.To` нет единой семантики. `DashboardService.cs:59` трактует границу как включающую, остальной код — как исключающую. Для `Day` получается `From == To` → **0018**.
- **С:** `DateTime.Today` в `DashboardService.cs:23` → **0003**.
- **Н:** `"Списание"`/`"Пополнение"` заданы литералами, тип платежа вычисляется в двух местах. Фильтры по категориям выполняются в памяти. `IEnumerable` вместо `IReadOnlyList`. `CsvFileFormat` парсит в зависимости от текущей культуры.

### 2.4. Infrastructure (§5)
- **В:** синхронный Dapper:
  - `RepositoryBase.cs:23,37`, то есть все Update/Delete;
  - `PaymentSourceRepository.cs:31,51,71,178`;
  - `AccountRepository.cs:71`, `TagRepository.cs:69`.
  → **0015**.
- **С:** id после вставки получают не через `OUTPUT INSERTED`, а через табличную переменную или `SCOPE_IDENTITY()` (`Account`, `Tag`, `PaymentSource`, `Payment`) → **0015**.
- **С:** продублированные SELECT'ы — 4 копии в `CategoryRepository` и 4 в `PaymentSourceRepository`. Два SELECT'а платежей разошлись: `PaymentRepository.cs:58` содержит алиас `..._ParentCategoryCategoryId`, а в DTO поле называется `..._ParentCategoryId`, поэтому значение всегда null → **0015**.
- **Н:** `dynamic`-маппинг вместо `QueryAsync<T>` → **0015**.
- **С:** нет транзакций. Их негде открыть: `IDbConnectionFactory` их не поддерживает. Касается `SetPaymentTags` (DELETE+INSERT) и `TagRepository.Delete` → **0016**.
- **С:** дерево категорий строится рекурсивными запросами (N+1). Знак суммы правится в `Infrastructure/Payments/PaymentDto.cs:66-69`, то есть доменное правило лежит в слое данных. `WITH (NOLOCK)` в выборках, по которым ищутся дубли → **0016**.
- **С:** `GetBySource` выбирает платежи без `IsDeleted = 0` → **0008**.

### 2.5. Website / API (§6)
- **В:** логика в `PaymentsApiController` → **0018**:
  - поля-состояние (`:32-33`);
  - дефолтный период через `DateTime.Today` (`:47-53`);
  - фейковые категории (`:58-60`);
  - `BuildTree` (`:218-289`), `ToPeriodKey` (`:291-313`);
  - повтор логики `CalculatedCategory` (`:165-179`);
  - правила правки платежа (`:89-98`).
- **С:** логика в `DashboardApiController`: проценты, `Math.Abs`, дефолтные диапазоны, копия `GetWeekStart` → **0018**.
- **В:** нет глобального обработчика ошибок с ProblemDetails, есть только `UseDeveloperExceptionPage` → **0017**. Последствия:
  - NRE в `EditPayment` для несуществующего id;
  - `Update` у Accounts/Tags/PaymentSources возвращает 200 для несуществующего id.
- **С:** наружу отдаются доменные сущности (`CategoriesApi`, `PaymentsApi.GetBySource`). Маппинг `Payment → PaymentDto` написан дважды, по-разному. Анонимные типы в ответах → **0018**.
- **С:** `"on"`-строки вместо `bool` (`PaymentEditDto.ExcludeFromStatsStr`, `CategoryDto`). Нет DataAnnotations. `[FromBody]`/`[FromRoute]` расставлены непоследовательно → **0017** (валидация), **0018** (контракт DTO).
- **Н:** DTO лежат в технической папке `RequestDto/` и в файлах с несколькими типами (`DashboardSummaryDto.cs`). `Ok()` используется вместо `NoContent()`. В `PaymentsApiController.cs:158-159` магические годы `1753`/`9999` → **0018**.
- **С:** в Website не включены `Nullable`/`ImplicitUsings`, что даёт CS8632. TFM: Website net9, Domain/Application/Infrastructure net7, Common net6. Используется legacy `System.Data.SqlClient` → **0013**.
- **Н:** в git лежит `appsettings.Production.json` со строкой подключения к prod-базе (секретов нет, Windows-auth) — нужно решение пользователя. CORS-origins захардкожены → **0012**.

### 2.6. Сквозное
- **В:** нет тестов. Нет `FinCtrl/tests/` и `InternalsVisibleTo`, а все сервисы `internal`. Логика зависит от `DateTime.Now` → **0003**.
- **С:** `ILogger` не используется ни разу, хотя NLog подключён. XML-doc только в 2 местах. File-scoped namespace не используется. Это подтягивается постепенно в затронутых файлах, как разрешает гайд. Логи импорта — в **0016**.
- **С:** документация расходилась с кодом: описывала Dommel/FluentMap, которых нет, и «THREE» SELECT'а вместо двух. Исправлено в этом PR.

## 3. Frontend
- **Шаблон Vite:**
  - `src/App.vue:2-4` — мёртвые импорты `HelloWorld`, `TheWelcome`, `PaymentsTable`;
  - `src/components/HelloWorld.vue`, `TheWelcome.vue`, `WelcomeItem.vue`, `icons/*`, `assets/logo.svg`;
  - пустой `pages/paymentsTable/Style.css`, неиспользуемый `utils/sortUtils.js`, шаблонный `README.md`, `index.html` с `lang=""`.
  → **0019**.
- **`assets/base.css`** — шаблонная тема Vue с тёмной темой через `prefers-color-scheme`. Её удаление меняет внешний вид, поэтому решение отложено → **0022**.
- **`console.log`** — `PaymentsTable.vue:395,523,528,532,553,565`, debug-`watch` в `:576-578` → **0019**.
- **Два Bootstrap (С):**
  - `index.html` грузит CSS и JS 5.0.2 с CDN, в `package.json` указан `^5.3.8`;
  - `Categories`/`PaymentSources` используют `import { Modal }`, `Payments`/`PaymentsTable` — глобальный `bootstrap.Modal`.
  → **0020**.
- **Дублирование (С):**
  - `formatCurrency` — 6 копий, `formatDate` — 6 копий с разными форматами;
  - `PaymentsTable.vue:364-368` форматирует дату через `toISOString()`, это UTC и возможный сдвиг на день;
  - `config.js` содержит захардкоженный `http://localhost:5000`.
  → **0021**.
- **Стайлгайд (Н):**
  - Options API в `PaymentCategoryRow.vue`, `PaymentList.vue`, `PaymentSourcesList.vue`;
  - около 30 вызовов `alert()` вместо error-состояния;
  - в `PaymentsTable` нет спиннера, модалки без `modal-lg`.
  → **0022**.
- **Тестов нет (В):** нет ни Vitest, ни скрипта `test` → **0004**.

## 4. Баги, найденные попутно
| Задача | Где | Что |
|---|---|---|
| 0005 | `MainStatisticService.cs:66-81`, `PaymentsApiController.ToPeriodKey`, `PaymentsTable.vue:323` | Группировка «Квартал» есть в UI, а backend отвечает `NotImplementedException` → 500 |
| 0006 | `PaymentSources.vue:394-404` ↔ `PaymentSourcesApiController.cs:46` | Кнопка 💾 шлёт `PUT /api/PaymentSourcesApi/{id}`, а маршрут описан только как `[HttpPut]` без id, и `paymentSourceId` в теле нет. Одиночное сохранение не работает |
| 0007 | `PaymentsTable.vue:379-384, 390` | `formatPeriodHeader` всегда разбирает ключ как `MMyyyy`, поэтому для Недели/Года/Дня заголовки неверные. Сравнение `!== '0'` выполняется между числом и строкой |
| 0008 | `PaymentRepository.GetBySource:275-283` | Удалённые платежи показываются в модалке источника |
| 0009 | `PaymentSourceService.cs:29,42,55,93-109` | `MapCategories` вызывается без `await`: исключения теряются, плюс `.Result` и N+1 |

Ещё один хрупкий контракт: `PaymentsTable.vue:479-486` не отправляет `ExcludeFromStatsStr`, поэтому PUT сбрасывает флаг в `false`. Сейчас это не проявляется. Учитывается в **0018** при переходе на `bool` и семантику «null = не трогать».

## 5. Бэклог и порядок

| ID | Ветка | Зависит от |
|---|---|---|
| 0003 | `chore/0003-backend-test-infra` | — |
| 0004 | `chore/0004-frontend-test-infra` | — |
| 0005 | `fix/0005-quarter-grouping` | 0003, 0004 |
| 0006 | `fix/0006-payment-source-single-save` | 0003 |
| 0007 | `fix/0007-period-header-format` | 0004 |
| 0008 | `fix/0008-source-payments-deleted` | — (SQL, ручная проверка) |
| 0009 | `fix/0009-payment-source-category-map` | 0003 |
| 0010 | `refactor/0010-remove-dead-projects` | — |
| 0011 | `refactor/0011-remove-legacy-mvc` | 0010 (желательно) |
| 0012 | `chore/0012-backend-code-cleanup` | 0011 |
| 0013 | `chore/0013-backend-net9-packages` | 0012 |
| 0014 | `refactor/0014-layer-dependencies` | 0003, 0011 |
| 0015 | `refactor/0015-async-repositories` | 0014 |
| 0016 | `refactor/0016-db-transactions` | 0015 |
| 0017 | `refactor/0017-errors-validation` | 0003, 0014 |
| 0018 | `refactor/0018-thin-api-controllers` | 0003, 0014, 0017 |
| 0019 | `refactor/0019-frontend-template-cleanup` | — |
| 0020 | `refactor/0020-frontend-single-bootstrap` | 0019 |
| 0021 | `refactor/0021-frontend-shared-utils` | 0004, 0019 |
| 0022 | `refactor/0022-frontend-styleguide` | 0020, 0021 |

Порядок:
1. Сначала тестовая инфраструктура (0003, 0004), затем баги.
2. Удаление мёртвого кода (0010–0013) уменьшает объём кода для архитектурных задач, поэтому его лучше сделать до них.
3. Цепочки 0014 → 0018 и 0019 → 0022 правят одни и те же файлы, их делать последовательно.

Без отдельных задач, постепенно в затронутых файлах:
- XML-doc на публичных методах и action'ах;
- file-scoped namespace;
- `ILogger` вне импорта;
- `CancellationToken` в долгих операциях.
