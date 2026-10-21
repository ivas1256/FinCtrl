# [0005] Группировка «Квартал» падает с 500

- **Ветка:** `fix/0005-quarter-grouping`
- **Репозитории:** backend, frontend
- **Создана:** 2026-10-20
- **Источник:** `.ai-docs/project/architecture-audit.md`, §4

## Контекст
На главной (`PaymentsTable.vue:324`) есть группировка «Квартал» (value 3 = `GroupingType.Quartal`). На backend `GroupingType.Quartal` не обрабатывается ни в `MainStatisticService` (`GetDateForGrouping`/`GetEndOfPeriod`), ни в `PaymentsApiController.ToPeriodKey`, поэтому вылетает `NotImplementedException` и ответ 500. Заголовки периодов после 0007 формируются в `src/utils/periodHeader.js` (`formatPeriodHeader`); для квартала там срабатывает fallback «дата – дата».

## Постановка
- Реализовать квартальную группировку: расчёт периода в `MainStatisticService` (начало — 1-е число первого месяца квартала, конец — исключающая граница +3 месяца, как у месяца), ключ периода в `ToPeriodKey`, формат заголовка в `formatPeriodHeader`.
- Реализация оказалась простой, поэтому вариант «убрать опцию из UI» не понадобился (см. журнал решений в `plan.md`).

## Вне объёма
- Перенос `BuildTree`/`ToPeriodKey` в Application (0018).

## Критерии приемки
- [x] Сначала написан красный тест на квартальный период в `MainStatisticService`
- [x] Выбор «Квартал» на главной показывает колонки по кварталам без ошибок
- [x] Заголовки кварталов читаемы (например «I кв. 2026»)
- [x] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
- [x] Все тесты зелёные
