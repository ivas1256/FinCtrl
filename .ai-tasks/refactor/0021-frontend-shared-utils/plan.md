# План: [0021] Общие форматтеры и адрес API из env

> Статус: `запланирована`. При старте сверить план с текущим кодом (номера строк из аудита могли сместиться) и обновить.

## Затрагиваемые части
- `src/utils/format.js` + spec
- `Payments.vue`, `PaymentsTable.vue`, `PaymentList.vue`, `PaymentSourcesList.vue`, `utils/heatmap.js`, `Dashboard.vue`, `PaymentSources.vue`
- `config.js`, `.env.example`, `CLAUDE.md`

## Шаги
- [ ] format.js + тесты
- [ ] Замена копий
- [ ] env для API
- [ ] Ручная проверка
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
Уточнить при старте задачи (регламент §3).

## Ручная проверка
Сценарии из критериев приемки — в preview `backend` + `frontend` / на dev-базе.

## Риски и открытые вопросы
-

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
