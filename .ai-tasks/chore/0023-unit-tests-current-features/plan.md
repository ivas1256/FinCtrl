# План: [0023] Юнит-тесты на текущий функционал

> Статус: `в работе`. Постановка актуализирована при старте по итогам инвентаризации (см. «Журнал решений»).

## Затрагиваемые части
- БД: нет
- Backend:
  - `FinCtrl/tests/FinCtrl.Domain.Tests/` — `AccountTests`, `TagTests`, `PaymentSourceTests`, дополнение `PaymentTests`, `CategoryTests`
  - `FinCtrl/tests/FinCtrl.Common.Tests/` — новый проект (xUnit), `DateRangeTests`, `DateTimeExtensionsTests`; добавить в `FinCtrl.Backend.sln`
  - `FinCtrl/tests/FinCtrl.Application.Tests/` — дополнение `CategoryServiceTests`, `MainStatisticServiceTests`, `DashboardServiceTests`,
    `PaymentServiceTests`, `PaymentsImportServiceTests`
  - `FinCtrl/tests/FinCtrl.Infrastructure.Tests/` — `Payments/PaymentDtoTests`, `Payments/PaymentTagRowTests`,
    `PaymentSources/PaymentSourceRowTests`, дополнение `CsvFileFormatTests`, `XlsxFileFormatTests`
  - `FinCtrl/tests/FinCtrl.Website.Tests/` — `ErrorHandling/ApiExceptionHandlerTests`, тесты контроллеров
    (`PaymentsApiControllerTests`, `DashboardApiControllerTests`, `PaymentSourcesApiControllerTests`, `AccountsApiControllerTests`,
    `TagsApiControllerTests`), DTO (`DashboardSummaryDtoTests`, `ChartDataDtoTests`, `PaymentSourceDtoTests`); пакеты NSubstitute,
    `Microsoft.Extensions.TimeProvider.Testing`
- Frontend: `src/components/CategoryTreeSelect.spec.js`, `src/pages/categories/CategoryNode.spec.js`, дополнение
  `src/utils/heatmap.spec.js`, `src/utils/periodHeader.spec.js`
- Документация: `CLAUDE.md` (список тестовых проектов), `backend-guide.md` (раздел 8), `project-summary.md`

## Шаги
- [x] 1. Инвентаризация существующих тестов, актуализация `about.md`/`plan.md`
- [ ] 2. Domain: фабрики, `Updated()`, `FromExcelPayment`, конструктор `Payment`, `Category.Equals`/fake-категории
- [ ] 3. `FinCtrl.Common.Tests`: `DateRange`, `DateTimeExtensions`
- [ ] 4. Application: `CategoryService.GetAllChildCategories`, `MainStatisticService.Calculate`, `DashboardService`,
  `PaymentService.BulkAssignTags`, `PaymentsImportService.Upload`
- [ ] 5. Infrastructure: маппинги строк выборки, дополнительные сценарии `CsvFileFormat`/`XlsxFileFormat`
- [ ] 6. Website: `ApiExceptionHandler`, контроллеры, DTO ответов
- [ ] 7. Frontend: `CategoryTreeSelect`, `CategoryNode`, `heatmap.js`, `periodHeader.js`
- [ ] 8. Документация: `CLAUDE.md`, `backend-guide.md`, `project-summary.md`
- [ ] Сборка + тесты (Definition of Done)
- [ ] PR

## Тесты
| Что тестируем | Сценарии |
|---------------|----------|
| Domain-фабрики | `CreateNew` (имя, `CreatedAt`, у счёта `DisplayName` = имя из файла, у источника нет категории), `CreateFromExisting`, `Updated()` у всех сущностей |
| `Payment.FromExcelPayment` | сумма < 0 и = 0 → `Spending`, > 0 → `Income`; источник, счёт, дата, `CreatedAt` |
| `Category` | `Equals` по id (в т.ч. с разными именами), не равна `null`/другому типу, `GetHashCode`; фейковые категории — имена и `OrderIndex` |
| `DateRange` / `DateTimeExtensions` | дни включительно → исключающая `To`, время отбрасывается; равенство и хэш; `CompareTo` по `From`, затем `To`, с `null`; начало следующего месяца (в т.ч. декабрь, 31-е число), первое число месяца |
| `CategoryService.GetAllChildCategories` | корень с 3 уровнями (порядок BFS), лист → пусто, вложенный узел, неизвестный id → пусто |
| `MainStatisticService.Calculate` | неделя с понедельника (воскресенье — в прошлой неделе), день, год; категория источника; своя категория приоритетнее; «Без категории»; итог «ВСЕГО» без доходных/«Накоплений»/«Без категории»; порядок по `OrderIndex`; `tagIds` → `GetByDateRange` |
| `DashboardService` | месяцы: подписи ru-RU, границы месяцев, пустые месяцы; в графике только траты; границы недели включительно; 1-е число месяца; воскресенье; `TopPayments` — поля и максимум 3; категории — сортировка по сумме, затем по имени; категория источника |
| `PaymentService.BulkAssignTags` | совпадение по категории источника; платежи без категории пропускаются; нет совпадений → 0 |
| `PaymentsImportService.Upload` | поток закрыт после разбора и при ошибке разбора; строка без счёта — счёт не ищется, платёж без счёта; тип «Перевод» — по знаку суммы; «Списание» с положительной суммой; дубль без счёта + строка без счёта → пропуск |
| Маппинги Infrastructure | `PaymentDto.MapToPayment` — полный маппинг, всё `null`, источник без категории, нормализация знака расхода; `PaymentSourceRow` — с категорией (цвет по умолчанию) и без; `PaymentTagRow` |
| Парсеры | CSV: тип пустой, счёт `null`, пустые строки; XLSX: книга без листов, пустая ячейка счёта |
| `ApiExceptionHandler` | 400/404/409 с `detail`; 500 в Production без подробностей, в Development с сообщением; отмена клиентом → 499 без тела; писатель отказался → JSON `application/problem+json` |
| Контроллеры | `PaymentsApi.Get` (обе даты → включительно, одна → `null`), `GetList` (`to` + 1 день, время отбрасывается), `BulkAssignTags`, `EditPayment` (id из роута, 204); `DashboardApi.GetChart`; `PaymentSourcesApi` — legacy `PUT` с id из тела и новый с id из роута; `AccountsApi.Create` (`DisplayName` из DTO, `CreatedAt` из `TimeProvider`), `Update` (id из роута); `TagsApi.Create`/`Update` |
| DTO ответов | `DashboardSummaryDto`, `ChartDataDto`, `PaymentSourceDto` (категория → `CategoryId`) |
| `CategoryTreeSelect.vue` | отступы `-`/`--` по уровню; категории из props — без запроса; `autoLoad` — запрос `getTree`; ошибка загрузки → пустой список; выбор → число, пустое → `null` |
| `CategoryNode.vue` | дети выводятся рекурсивно, сворачиваются; события `edit`/`delete`/`add-child` всплывают от детей; бейдж «Только доход»; цвет по умолчанию |
| `heatmap.js` / `periodHeader.js` | `heatClass` при `maxV = 0`, `computeMaxValue` для узлов без `periods`/`totalSum`; группировка не выбрана → диапазон дат |

## Ручная проверка
Не требуется: код приложения не меняется. Проверка — `dotnet build`/`dotnet test FinCtrl.Backend.sln`, `npm run build`/`npm run test`.

## Риски и открытые вопросы
- Подписи месяцев графика зависят от данных культуры `ru-RU` (ICU) — на Windows и в CI с ICU стабильны; в тесте сравнивается
  с `ToString("MMM yy", ru-RU)`, а не с захардкоженной строкой, чтобы не зависеть от версии ICU.
- `ApiExceptionHandler` тестируется на `DefaultHttpContext` с моком `IProblemDetailsService` — без поднятия хоста.

## Журнал решений
- 2026-10-20: задача заведена по запросу пользователя: зафиксировать тестами текущее поведение перед рефакторингами 0014–0018, 0021.
- 2026-10-20: при старте — отступление от п. 1.2 регламента: ветка создана не от `main`, а от `refactor/0022-frontend-styleguide`
  (стек PR #9→…→#25 не смёржен, тесты пишутся на код после 0022). PR — с базой `refactor/0022-frontend-styleguide`, мёржить после #25.
  Хук pre-pr-check смотрит дифф `main...HEAD` — в нём будут и изменения стека, это ожидаемо.
- 2026-10-20: инвентаризация — постановка устарела. Уже покрыто тестами профильных задач (не дублируем): семантика null правки
  платежа (`PaymentService.Edit`, `EditPaymentRequest.ToModel`), дерево и ключи периодов главной (`MainStatisticService.GetPaymentsTable`,
  `GetPaymentsTableResponse`), квартал/месяц в `Calculate`, маппинг `PaymentDto` (Website), get-or-create/дубли/транзакция импорта,
  `FileFormatFactory`, основные сценарии CSV/XLSX, валидация и конфликты `Account`/`Tag`/`Category`/`PaymentSource`-сервисов,
  `GetCategoriesTree`, `Payment.CalculatedCategory`/`NormalizeSumSign`, `Category.IsIncludeInTotalResults`, frontend-утилиты
  (`format`, `apiError`, `heatmap`, `periodHeader`, `useAlerts`) и компоненты главной. Устаревшие пункты: `EditPayment`/`BuildTree`
  в контроллере (логика в сервисах), `sortUtils.js` (удалён), `MapCategories` (удалён в 0009), создание `FinCtrl.Website.Tests` (есть).
  Новая постановка — только непокрытые места (см. `about.md`).
- 2026-10-20: для `DateRange`/`DateTimeExtensions` заведён проект `FinCtrl.Common.Tests` — по `backend-guide.md` (раздел 8) у каждого
  тестируемого проекта свой тестовый проект. `Paging.Validate` (`FinCtrl.Common.Application`) отдельного проекта не получает: он уже
  проверяется через `GetListPaged_InvalidPaging_*` сервисов, заводить ради одного метода проект незачем.
- 2026-10-20: `ExcludeFromStats` учитывается только в SQL (`PaymentRepository`), поэтому юнит-тестами не покрывается
  (проверка SQL — вручную по регламенту).

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
