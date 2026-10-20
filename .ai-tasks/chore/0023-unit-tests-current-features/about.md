# [0023] Юнит-тесты на текущий функционал

- **Ветка:** `chore/0023-unit-tests-current-features`
- **Репозитории:** backend `FinCtrl/`, frontend `FinCtrl.Frontend/`
- **Создана:** 2026-10-20
- **Зависит от:** 0003 (тестовая инфраструктура backend), 0004 (тестовая инфраструктура frontend)

## Контекст
Задачи 0003/0004 заводят тестовую инфраструктуру и пишут только «первые» тесты: `Payment.CalculatedCategory`, `Category.IsIncludeInTotalResults`, один сценарий `DashboardService`, `utils/heatmap.js`. Остальная бизнес-логика тестами не покрыта. Впереди рефакторинги 0014–0018 (слои, async-репозитории, транзакции, валидация, тонкие контроллеры) и 0021 (фронтовые утилиты). Без страховочной сетки из тестов изменения поведения при этих рефакторингах не заметить.

Цель — зафиксировать тестами **текущее** поведение работающего функционала (characterization tests), чтобы рефакторинги шли под их защитой.

## Постановка
Покрыть юнит-тестами (моки репозиториев/сервисов, без БД, сети и текущего времени) существующую логику.

**Backend — `FinCtrl/tests/FinCtrl.Application.Tests/`:**
- `PaymentsImportService.Upload`:
  - get-or-create `PaymentSource` и `Account` по имени: существующий берётся, новый создаётся;
  - определение `PaymentType` по типу строки («Пополнение» → `Income`, расход → `Spending`);
  - поиск дублей по сумме + дате: новый платёж создаётся; у найденного дубля с пустым `Account` счёт дозаполняется через `Update`; у дубля с заполненным `Account` ничего не меняется;
  - выбор формата через `FileFormatFactory`, закрытие потока в `finally`.
- `FileFormatFactory`: `.xlsx`/`.csv` → нужный формат, неизвестное расширение → текущее поведение (исключение/иное — зафиксировать как есть).
- `CsvFileFormat` и `XlsxFileFormat`: парсинг небольших фикстур в тестовом проекте (маппинг колонок, суммы, даты, тип операции, имя счёта, пустые/лишние строки).
- `MainStatisticService.Calculate`: группировка по дню/неделе/месяцу/году, начало и конец периода, учёт итоговой категории (`PaymentCategory ?? PaymentSource.PaymentSourceCategory`), исключение `ExcludeFromStats`, фильтр по тегам (передача в `GetByDateRange`).
- `DashboardService`: `GetSummary` (суммы, траты по категориям с полными именами из дерева), `GetChartData` для режимов день/неделя/месяц, включая дни без трат и границы диапазона; время — через `TimeProvider` из 0003.
- `CategoryService`: `GetAllChildCategories` (корень, лист, глубокая вложенность, несуществующий id), построение/уплощение дерева.
- `PaymentService`: `Update`/`BulkAssignTags`/`SetPaymentTags` — проброс в репозиторий и вызов `Updated()`.
- `PaymentSourceService`: подстановка категорий источникам (`MapCategories`), `GetListPaged`.
- `AccountService`, `TagService`: `Update` вызывает `Updated()` и передаёт сущности в репозиторий; `Exists`/`GetBy*` пробрасываются.
- Domain: фабрики `CreateNew`/`CreateFromExisting`, `Payment.FromExcelPayment`, `Updated()`, `Category.Equals`/`GetHashCode`, `CreateFakeEmpty`/`CreateFakeTotal`.

**Backend — `FinCtrl/tests/FinCtrl.Website.Tests/`** (создать, если ещё нет; добавить в `FinCtrl.Backend.sln`):
- `PaymentsApiController.EditPayment` — семантика null в `PaymentEditDto`: `CategoryId == null` → не трогать; `AccountId` null → не трогать, `0` → очистить, иное → установить; `TagIds` null → не трогать, список → заменить целиком; `paymentId` берётся из тела.
- `PaymentsApiController.Get` / `BuildTree` — построение дерева категорий × периоды: суммы по периодам, свёртка детей в родителя, фейковые категории «Без категории»/«Итого», ключи периодов (`ToPeriodKey`, `GetWeekNumber`).
- `PaymentsApiController.GetList` — маппинг `Payment` → `PaymentDto` (включая теги и итоговую категорию).

**Frontend — Vitest:**
- `src/utils/sortUtils.js` — `insertionSort`, `insertionSortWithComparator` (пустой массив, один элемент, дубли, обратный порядок, стабильность).
- `src/utils/heatmap.js` — дополнить тесты из 0004 граничными случаями, если они там не покрыты (`maxV = 0`, отрицательные значения, пустое дерево в `computeMaxValue`, `formatHeatCurrency` для 0/дробных/больших чисел).

## Вне объёма
- Исправление найденных багов. Если тест выявил ошибку (в т.ч. уже известные 0005–0009), тест фиксирует текущее поведение с комментарием-ссылкой на задачу либо не пишется для этого сценария; баг заносится в «Вне объёма» `plan.md` / отдельной задачей. Код приложения в этой задаче не меняется, кроме минимально необходимого для тестируемости (по согласованию, с записью в журнал решений).
- SQL репозиториев (`FinCtrl.Infrastructure`) — по регламенту проверяется вручную, юнит-тестами не покрывается.
- Вынос логики из Vue-компонентов в утилиты/composables (задача 0021) и тесты компонентов `.vue`.
- Тривиальные pass-through без логики сверх одного вызова репозитория — не обязательны.
- Тестовая инфраструктура (0003, 0004) и `TimeProvider` (0003).
- Метрики покрытия / порог coverage в CI.

## Критерии приемки
- [ ] Есть тесты на `PaymentsImportService` (get-or-create источника/счёта, тип операции, три ветки обработки дублей)
- [ ] Есть тесты на `FileFormatFactory`, `CsvFileFormat`, `XlsxFileFormat` на фикстурах из тестового проекта
- [ ] Есть тесты на `MainStatisticService.Calculate` для всех типов группировки и на итоговую категорию / `ExcludeFromStats`
- [ ] Есть тесты на `DashboardService` (`GetSummary`, `GetChartData` во всех режимах) с фиксированным временем
- [ ] Есть тесты на `CategoryService.GetAllChildCategories` и на логику `PaymentService`, `PaymentSourceService`, `AccountService`, `TagService`
- [ ] Есть тесты на Domain-фабрики и `Updated()`
- [ ] Создан `FinCtrl.Website.Tests` (в .sln), есть тесты на семантику null в `EditPayment`, `BuildTree` и маппинг `GetList`
- [ ] Есть тесты на `sortUtils.js` и граничные случаи `heatmap.js`
- [ ] Тесты детерминированные: без БД, сети, `DateTime.Now`
- [ ] Код приложения не изменён (или изменения согласованы и записаны в журнал решений `plan.md`)
- [ ] Найденные при написании тестов баги перечислены в `plan.md` («Вне объёма») и сообщены пользователю
- [ ] `dotnet test FinCtrl.Backend.sln` и `npm run test` зелёные
- [ ] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
