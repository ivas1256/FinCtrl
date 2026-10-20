# План: [0005] Группировка «Квартал» падает с 500

> Статус: `в работе`.

## Затрагиваемые части
- `FinCtrl/src/main/FinCtrl.Application/Statistic/MainStatisticService.cs` (`GetDateForGrouping`, `GetEndOfPeriod`)
- `FinCtrl/FinCtrl.Website/Controllers/PaymentsApi/PaymentsApiController.cs` (`ToPeriodKey`)
- `FinCtrl/tests/FinCtrl.Application.Tests/Statistic/MainStatisticServiceTests.cs` (новый)
- `FinCtrl.Frontend/FinCtrl.Frontend/src/utils/periodHeader.js` (`formatPeriodHeader`, вынесена из `PaymentsTable.vue` задачей 0007) + `periodHeader.spec.js`

## Шаги
- [ ] Красный тест
- [ ] Реализация периода и ключа
- [ ] Заголовок на frontend
- [ ] Ручная проверка на главной
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

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

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
