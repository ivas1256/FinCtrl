# План: [0005] Группировка «Квартал» падает с 500

> Статус: `на ревью`.

## Затрагиваемые части
- `FinCtrl/src/main/FinCtrl.Application/Statistic/MainStatisticService.cs` (`GetDateForGrouping`, `GetEndOfPeriod`)
- `FinCtrl/FinCtrl.Website/Controllers/PaymentsApi/PaymentsApiController.cs` (`ToPeriodKey`)
- `FinCtrl/tests/FinCtrl.Application.Tests/Statistic/MainStatisticServiceTests.cs` (новый)
- `FinCtrl.Frontend/FinCtrl.Frontend/src/utils/periodHeader.js` (`formatPeriodHeader`, вынесена из `PaymentsTable.vue` задачей 0007) + `periodHeader.spec.js`

## Шаги
- [x] Красный тест
- [x] Реализация периода и ключа
- [x] Заголовок на frontend
- [x] Ручная проверка на главной
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [x] PR

## Тесты
- `MainStatisticServiceTests` (xUnit + NSubstitute, `IPaymentService` замокан): платежи разных месяцев одного квартала попадают в один период `[1-е число квартала; 1-е число следующего квартала)`; платежи разных кварталов — в разные периоды, в т.ч. IV кв. с границей 01.01 следующего года; регрессия для месяца. Сначала тест красный (`NotImplementedException`).
- `periodHeader.spec.js`: квартал — «I кв. 2026», «IV кв. 2025».
- `ToPeriodKey` (приватный метод контроллера) юнит-тестами не покрывается: тестового проекта для Website нет, перенос в Application — задача 0018. Проверяется вручную через API.

## Ручная проверка
- `GET api/PaymentsApi?periodType=3&...` на dev-базе — 200, ключи периодов вида `Q42025`.
- Главная в preview `backend` + `frontend`: выбрать «Квартал» — колонки по кварталам, заголовки «IV кв. 2025», нет ошибок в консоли браузера и логах backend.

## Риски и открытые вопросы
- Пересекалась с 0007 (`formatPeriodHeader`). 0007 уже смёржена, работаем поверх неё.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: реализуем квартал, а не убираем опцию из UI: правка в трёх местах, отдельное согласование с пользователем не требуется.
- 2026-10-20: конец периода квартала — исключающая граница `start.AddMonths(3)`, как у месяца и года.
- 2026-10-20: ключ периода квартала — `Q<N><yyyy>` (например `Q12026`). Ключи нужны только как идентификаторы колонок, сортировка на frontend идёт по `from`.
- 2026-10-20: заголовок — римская цифра квартала: «I кв. 2026».
- 2026-10-20: в `PaymentsApiController` убраны аннотации `?` у `int[]? tagIds` (2 места): в файле нет nullable-контекста, они давали старые предупреждения CS8632, а хук pre-pr-check блокирует любые предупреждения в изменённых файлах. Поведение не меняется — массив и так допускает null.
- 2026-10-20: ручная проверка проведена на периоде с 01.10.2025 — на периоде с 01.01.2025 API падает с 500 по не связанной с задачей причине (см. «Вне объёма»).

## Вне объёма (найдено попутно)
- `PaymentRepository.GetTagsForPayments` передаёт все id платежей одним `WHERE pt.PaymentId IN @Ids`. Если в периоде больше ~2100 платежей, SQL Server отвечает «The incoming request has too many parameters» и `GET api/PaymentsApi` возвращает 500 при любой группировке (на dev-базе — `from=2025-01-01`). То же касается `GetListPaged` при большом размере страницы. Предлагается отдельная fix-задача: батчить id по ~1000 или передавать их через TVP/временную таблицу.
- В `PaymentsTable.vue` остались отладочные `console.log` (`loadPayments` и др.) — относится к 0019/0022.

## Итог
- Backend: `MainStatisticService` считает квартальный период, `ToPeriodKey` даёт ключ `Q<N><yyyy>`; 3 юнит-теста (`MainStatisticServiceTests`), сначала красные (2 из 3 падали с `NotImplementedException : Quartal`).
- Frontend: `formatPeriodHeader` для квартала — «I кв. 2026»; 2 теста в `periodHeader.spec.js`.
- Проверено: `dotnet build` (0 ошибок, нет предупреждений в изменённых файлах), `dotnet test` (8 + 5 зелёных), `npm run test` (23 зелёных), `npm run build`. Вручную в preview: `GET api/PaymentsApi?periodType=3` → 200, периоды `Q42025:2025-10-01..2026-01-01` … `Q42026`; на главной «Квартал» с 01.10.2025 показывает колонки «IV кв. 2025 … IV кв. 2026», ошибок в консоли нет.
- Осталось: см. «Вне объёма».
