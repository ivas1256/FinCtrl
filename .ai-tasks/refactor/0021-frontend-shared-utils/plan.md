# План: [0021] Общие форматтеры и адрес API из env

> Статус: `в работе`. План сверен с кодом после задач 0005, 0017–0020.

## Затрагиваемые части (frontend)
- `src/utils/format.js` + `format.spec.js` — новый модуль форматирования.
- Копии форматтеров (сверено с кодом):
  - `formatCurrency` — `Payments.vue`, `PaymentsTable.vue`, `PaymentList.vue`, `PaymentSourcesList.vue`, `Dashboard.vue` (`fmt`), `PaymentSources.vue` (инлайн `toLocaleString` + `₽`), `utils/heatmap.js` (`formatHeatCurrency` со знаком);
  - `formatDate` — `Accounts.vue`, `Tags.vue`, `PaymentSources.vue`, `Dashboard.vue` (+ `formatShortDate`), `Payments.vue` (дата + время), `PaymentList.vue` (`formatDisplayDate`, длинная), `PaymentsTable.vue` (для input — через `toISOString()`, баг);
  - `toInputDate` — `PaymentsTable.vue` (локальная, исправлена в 0005), `Dashboard.vue` (через `toISOString()`, тот же UTC-сдвиг);
  - `fmtShort` — `Dashboard.vue`.
- `config.js` → `src/config.js` с `import.meta.env.VITE_API_BASE_URL`; `.env.example`; импорты `APP_CONFIG` во всех компонентах.
- Документы: `CLAUDE.md`, `.ai-docs/workflow/styleguide.md`, `.ai-docs/project/project-summary.md`.

## Шаги
- [ ] Красный тест: `toInputDate` для даты платежа `"2026-10-20T00:00:00"` возвращает `2026-10-20` (баг модалки на главной — UTC-сдвиг `toISOString()`)
- [ ] format.js + тесты
- [ ] Замена копий
- [ ] env для API
- [ ] Ручная проверка
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
- `src/utils/format.spec.js` (Vitest): `formatCurrency` (копейки, отрицательные, null/undefined → 0), `formatSignedCurrency` (знак), `formatShortAmount` (к/М), `parseDate` (строка только с датой — локальная полночь), `formatDate`/`formatShortDate`/`formatDateTime`/`formatLongDate` (пустые значения → заглушка), `toInputDate` (Date и строка API без сдвига на день; тесты запускаются с `TZ` с положительным смещением, чтобы сдвиг воспроизводился независимо от машины).
- `src/config.spec.js`: дефолтный адрес без env и адрес из `VITE_API_BASE_URL`.
- `heatmap.spec.js` — `formatHeatCurrency` остаётся (алиас общего форматтера), тесты не меняются.
- Статический тест: в `.vue`-файлах нет локальных `formatCurrency`/`formatDate`/`Intl.NumberFormat`/`toISOString`.

## Ручная проверка
Preview `backend` + `frontend`: главная (тепловая карта, модалка ячейки — суммы; модалка правки платежа — сумма и дата совпадают с датой платежа), «Операции» (дата+время, суммы), «Дашборд» (суммы, даты, оси графика, диапазоны по умолчанию), «Источники» (даты, модалка платежей источника — суммы), «Счета», «Теги» (даты создания). Консоль браузера без ошибок.

## Риски и открытые вопросы
- Небольшие визуальные изменения форматов: в модалке платежей источника сумма теперь всегда с 2 знаками копеек и в общем формате валюты.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: «стек веток» — ветка создана от `refactor/0020-frontend-single-bootstrap` (PR #23 не смёржен), а не от `main` (отступление от п. 1.2 регламента); PR открывается в базу `refactor/0020-frontend-single-bootstrap`, мёржить после #23.
- 2026-10-20: в задачу добавлен отложенный баг — дата на день раньше в модалке правки платежа на главной (`toISOString()` в UTC). Тот же сдвиг найден в `Dashboard.vue` (`toInputDate` для диапазона по умолчанию) — исправляется заодно общей `toInputDate`.
- 2026-10-20: `config.js` перенесён в `src/config.js` (аудит: «лежит вне `src/`»); имя `APP_CONFIG.apiBaseUrl` сохранено, чтобы не менять вызовы axios.
- 2026-10-20: опциональный модуль загрузки справочников (`getTree`, теги, счета) не делается — требовал согласования и близок к API-слою, который вне объёма; см. «Вне объёма».
- 2026-10-20: `formatDate` принимает заглушку для пустого значения вторым параметром (по умолчанию `-`, как было на страницах-списках).

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
