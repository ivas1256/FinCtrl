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
- [x] 2. Domain: фабрики, `Updated()`, `FromExcelPayment`, конструктор `Payment`, `Category.Equals`/fake-категории
- [x] 3. `FinCtrl.Common.Tests`: `DateRange`, `DateTimeExtensions`
- [x] 4. Application: `CategoryService.GetAllChildCategories`, `MainStatisticService.Calculate`, `DashboardService`,
  `PaymentService.BulkAssignTags`, `PaymentsImportService.Upload`
- [x] 5. Infrastructure: маппинги строк выборки, дополнительные сценарии `CsvFileFormat`/`XlsxFileFormat`
- [x] 6. Website: `ApiExceptionHandler`, контроллеры, DTO ответов
- [x] 7. Frontend: `CategoryTreeSelect`, `CategoryNode`, `heatmap.js`, `periodHeader.js`
- [x] 8. Документация: `CLAUDE.md`, `backend-guide.md`, `project-summary.md`
- [x] Сборка + тесты (Definition of Done)
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

- 2026-10-20: в таблице тестов парсеров «XLSX: книга без листов» заменено на «строки после пустой даты, пустые ячейки, несколько
  листов»: EPPlus не сохраняет книгу без листов (`GetAsByteArray` бросает), собрать такую фикстуру без бинарного файла нельзя,
  а ветка `?? throw` тривиальна.
- 2026-10-20: найденные баги (см. «Вне объёма») не исправляются — по постановке тесты фиксируют текущее поведение
  (`Upload_RowWithoutSourceName_ThrowsArgumentNullExceptionWithoutCommit`, `Upload_RowWithEmptySourceName_CreatesSourceWithEmptyName`,
  `heatClass` при `maxV = 0`) с комментарием-ссылкой на этот план; задача на исправление перевернёт эти тесты.
- 2026-10-20: в `FinCtrl.Website.Tests` добавлены пакеты `NSubstitute` и `Microsoft.Extensions.TimeProvider.Testing` (те же версии,
  что в `FinCtrl.Application.Tests`) — для моков сервисов в тестах контроллеров и `IProblemDetailsService`.
- 2026-10-20: production-код не менялся — для тестируемости ничего не понадобилось (`internal`-классы Infrastructure доступны
  через существующий `InternalsVisibleTo`, контроллеры создаются напрямую с моками).

## Вне объёма (найдено попутно)
- **Баг: пустая ячейка источника в XLSX роняет импорт с 500.** `XlsxFileFormat` отдаёт `SourceName = null` для пустой ячейки
  колонки 7, `PaymentsImportService.GetOrCreatePaymentSource` кладёт имя ключом в `Dictionary` → `ArgumentNullException`
  (необработанное → 500 «Внутренняя ошибка сервера»), а не `ValidationException` с номером строки. Транзакция откатывается,
  данные не портятся. Предлагается отдельная `fix/`-задача: проверять имя источника в парсерах (или в импорте) и бросать
  `ValidationException` «Строка N: не указан источник».
- **Пустое имя источника создаётся молча.** Строка CSV с пустой колонкой источника (или ячейка XLSX из пробелов) создаёт
  `PaymentSource` с пустым именем — `PaymentSourceService.Create` не валидирует имя (в отличие от `Edit`). Решать вместе с багом выше.
- `heatClass(value, 0)` для расхода возвращает `hm-hi` (деление на 0 → `Infinity`). На практике недостижимо (`maxV` — максимум
  `|totalSum|`, при расходе он > 0), исправление не требуется; зафиксировано тестом.
- Техдолг `CategoryTreeSelect.vue`: пропсы `indentChar`, `indentSymbol`, `emptyOptionText` объявлены, но не используются (пустой
  вариант выводится без текста, отступ — всегда `-`), `category.disabled` нигде не задаётся, ошибка загрузки пишется в `console.error`
  без сообщения пользователю. Кандидат в задачу по стайлгайду/чистке компонентов.
- При импорте «Списание» с положительной суммой сохраняется с положительной суммой (знак выравнивает только `NormalizeSumSign` при
  чтении); поиск дублей идёт по сумме из файла. Поведение зафиксировано тестом; если банк начнёт так выгружать — стоит нормализовать
  знак уже при импорте.
- Логика страниц `src/pages/*/<Страница>.vue` (фильтры, пагинация, расчёт периодов «сегодня/неделя» через `new Date()`) юнит-тестами
  не покрыта — нужен вынос в утилиты/composables отдельной задачей.

## Итог
- Backend: 169 → 304 теста (+135): Domain 17 → 40, новый `FinCtrl.Common.Tests` — 16, Application 119 → 160, Infrastructure 24 → 41,
  Website 9 → 47. Frontend: 88 → 110 (+22), 14 → 16 файлов.
- Проверка: `dotnet build FinCtrl.Backend.sln` — 0 ошибок, 0 предупреждений; `dotnet test FinCtrl.Backend.sln` — 304/304;
  `npm run build` — успешно; `npm run test` — 110/110.
- Код приложения не менялся; найдены и зафиксированы тестами 2 дефекта импорта (см. «Вне объёма») — предложена отдельная fix-задача.
