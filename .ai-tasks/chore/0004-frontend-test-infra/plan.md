# План: [0004] Тестовая инфраструктура frontend

> Статус: `в работе`.

## Затрагиваемые части
- `package.json`, `package-lock.json`
- `vite.config.js`
- `src/utils/heatmap.spec.js`
- `CLAUDE.md`

## Шаги
- [x] Установить зависимости и настроить Vitest
- [x] Тесты на heatmap.js
- [x] Обновить CLAUDE.md
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
`src/utils/heatmap.spec.js` — 15 тестов: `catColor`, `categoryColor`, `heatAlpha` (ноль/поступление/границы шкалы), `heatStyle`, `heatClass` (порог 52%), `computeMaxValue` (вложенные строки, пустое дерево), `formatHeatCurrency` (знак, ноль).

## Ручная проверка
UI не менялся — достаточно `npm run build` и `npm run test`.

## Риски и открытые вопросы
- Совместимость версии Vitest с текущей версией Vite — выбрать соответствующую мажорную версию.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: Vitest 3.x (совместим с Vite 7), jsdom 26; конфиг тестов — в `vite.config.js`, отдельный `vitest.config.js` не нужен.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
