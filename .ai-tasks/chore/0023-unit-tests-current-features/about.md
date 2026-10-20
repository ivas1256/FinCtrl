# [0023] Юнит-тесты на текущий функционал

- **Ветка:** `chore/0023-unit-tests-current-features`
- **Репозитории:** backend `FinCtrl/`, frontend `FinCtrl.Frontend/`
- **Создана:** 2026-10-20 (постановка актуализирована при старте — см. «Журнал решений» в `plan.md`)
- **Зависит от:** 0003, 0004 (тестовая инфраструктура); стекается поверх 0022 (PR #25)

## Контекст
Задача заводилась до 0005–0022 как «страховочная сетка» перед рефакторингами. За это время рефакторинги прошли, и почти вся
бизнес-логика уже покрыта тестами, написанными в профильных задачах: backend — 169 тестов (`FinCtrl.Domain.Tests` 17,
`FinCtrl.Application.Tests` 119, `FinCtrl.Infrastructure.Tests` 24, `FinCtrl.Website.Tests` 9), frontend — 88 тестов (14 файлов).
Логика `EditPayment` переехала в `PaymentService.Edit`, дерево главной — в `MainStatisticService.GetPaymentsTable`,
`sortUtils.js` удалён, `FinCtrl.Website.Tests` уже существует.

Цель — по итогам инвентаризации закрыть **оставшиеся пробелы** в покрытии текущего функционала, не дублируя имеющиеся тесты:
зафиксировать текущее поведение (characterization tests).

## Постановка
Покрыть юнит-тестами (моки, без БД, сети и текущего времени — `FakeTimeProvider`) непокрытую логику:

**Backend — Domain (`FinCtrl.Domain.Tests`):**
- фабрики `Account`/`Tag`/`PaymentSource` (`CreateNew`, `CreateFromExisting`), `Updated()` всех сущностей;
- `Payment.FromExcelPayment` (тип по знаку суммы, поля), конструктор `Payment` с id;
- `Category.Equals`/`GetHashCode` (равенство по id), `CreateFakeEmpty`/`CreateFakeTotal`.

**Backend — Common (новый проект `FinCtrl.Common.Tests`, в `.sln`):**
- `DateRange`: `FromInclusiveDates`, `Equals`/`GetHashCode`, `CompareTo`;
- `DateTimeExtensions`: `NextMonthStart`, `FirstDayOfMonth`.

**Backend — Application (`FinCtrl.Application.Tests`):**
- `CategoryService.GetAllChildCategories`: корень с глубокой вложенностью, лист, вложенный узел, несуществующий id;
- `MainStatisticService.Calculate`: группировка по неделе/дню/году, итоговая категория (категория источника, своя приоритетнее),
  итог «ВСЕГО» без доходных/«Накоплений»/«Без категории», порядок по `OrderIndex`, проброс `tagIds`;
- `DashboardService`: `GetChartData` по месяцам (подписи, границы), учёт только трат, границы недель;
  `GetSummary` — первое число месяца (вчера — прошлый месяц), воскресенье, `TopPayments` (поля, не больше трёх), сортировка категорий,
  категория источника;
- `PaymentService.BulkAssignTags`: категория источника, платежи без категории, нет совпадений;
- `PaymentsImportService.Upload`: закрытие потока (успех и ошибка парсинга), строка без счёта, тип операции, не равный
  «Списание»/«Пополнение», дубль без счёта при строке без счёта.

**Backend — Infrastructure (`FinCtrl.Infrastructure.Tests`):**
- маппинги строк выборки: `PaymentDto.MapToPayment` (все ветки null, нормализация знака), `PaymentSourceRow.MapToPaymentSource`,
  `PaymentTagRow.MapToTag`;
- `CsvFileFormat`: тип операции и счёт не заполняются, пустые строки, BOM; `XlsxFileFormat`: строки после пустой даты, пустые ячейки, несколько листов.

**Backend — Website (`FinCtrl.Website.Tests`):**
- `ApiExceptionHandler`: 400/404/409/500 (Development/Production), 499 при отмене запроса клиентом, запись JSON, если писатель
  ProblemDetails отказался;
- логика контроллеров: преобразование дат «по включительно» (`PaymentsApi.Get`/`GetList`/`BulkAssignTags`, `DashboardApi.GetChart`),
  id из роута/тела (`PaymentsApi.EditPayment`, оба `PUT` `PaymentSourcesApi`), создание счёта/тега с `TimeProvider`
  (`AccountsApi`, `TagsApi`), маппинг ответов `DashboardSummaryDto`/`ChartDataDto`/`PaymentSourceDto`.

**Frontend — Vitest:**
- `CategoryTreeSelect.vue`: плоский список с отступами, загрузка дерева из API / из props, ошибка загрузки, `update:modelValue`
  (число / `null`);
- `CategoryNode.vue`: рекурсивный вывод детей, сворачивание, всплытие событий от дочерних узлов, бейдж дохода, цвет по умолчанию;
- `heatmap.js` (граничные случаи `heatClass`/`computeMaxValue`), `periodHeader.js` (группировка не выбрана).

## Вне объёма
- Исправление найденных багов — только запись в «Вне объёма» `plan.md` (тест фиксирует текущее поведение, без `Skip`).
- Изменение кода приложения — только минимально необходимое для тестируемости, с записью в журнал решений.
- SQL репозиториев (проверяется вручную по регламенту), `RepositoryBase`/`DbConnectionFactory` (нужна БД).
- Страницы `src/pages/*/<Страница>.vue` (логика внутри компонентов страниц — отдельные задачи на вынос в утилиты/composables).
- Тривиальные pass-through без логики сверх одного вызова, уже покрытое тестами, метрики покрытия.

## Критерии приемки
- [x] Инвентаризация существующих тестов проведена, постановка актуализирована, изменения записаны в журнал решений `plan.md`
- [x] Есть тесты на Domain-фабрики, `Updated()`, `FromExcelPayment`, равенство категорий и фейковые категории
- [x] Создан `FinCtrl.Common.Tests` (в `.sln`), есть тесты на `DateRange` и `DateTimeExtensions`
- [x] Есть тесты на `CategoryService.GetAllChildCategories`
- [x] Есть тесты на `MainStatisticService.Calculate` для группировок неделя/день/год, итоговую категорию, итог «ВСЕГО», проброс тегов
- [x] Есть тесты на `DashboardService.GetChartData` по месяцам и дополнительные сценарии `GetSummary` с фиксированным временем
- [x] Дополнены тесты `PaymentService.BulkAssignTags` и `PaymentsImportService.Upload` (поток, строка без счёта, тип операции)
- [x] Есть тесты на маппинги строк выборки Infrastructure и дополнительные сценарии парсеров
- [x] Есть тесты на `ApiExceptionHandler` и на логику контроллеров (даты, id из роута/тела, маппинг ответов)
- [x] Есть тесты на `CategoryTreeSelect.vue`, `CategoryNode.vue` и граничные случаи `heatmap.js`/`periodHeader.js`
- [x] Тесты детерминированные: без БД, сети, `DateTime.Now`
- [x] Код приложения не изменён (или изменения записаны в журнал решений `plan.md`)
- [x] Найденные при написании тестов баги перечислены в `plan.md` («Вне объёма») и сообщены пользователю
- [x] `dotnet test FinCtrl.Backend.sln` и `npm run test` зелёные
- [x] Сборка затронутых частей проходит без ошибок, нет предупреждений в изменённых файлах
