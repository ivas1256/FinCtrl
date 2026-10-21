# План: [0021] Общие форматтеры и адрес API из env

> Статус: `на ревью`. План сверен с кодом после задач 0005, 0017–0020.

## Затрагиваемые части (frontend)
- `src/utils/format.js` + `format.spec.js` — новый модуль форматирования.
- Копии форматтеров (сверено с кодом):
  - `formatCurrency` — `Payments.vue`, `PaymentsTable.vue`, `PaymentList.vue`, `PaymentSourcesList.vue`, `Dashboard.vue` (`fmt`), `PaymentSources.vue` (инлайн `toLocaleString` + `₽`), `utils/heatmap.js` (`formatHeatCurrency` со знаком);
  - `formatDate` — `Accounts.vue`, `Tags.vue`, `PaymentSources.vue`, `Dashboard.vue` (+ `formatShortDate`), `Payments.vue` (дата + время), `PaymentList.vue` (`formatDisplayDate`, длинная), `PaymentsTable.vue` (для input — через `toISOString()`, баг);
  - `toInputDate` — `PaymentsTable.vue` (локальная, исправлена в 0005), `Dashboard.vue` (через `toISOString()`, тот же UTC-сдвиг);
  - `fmtShort` — `Dashboard.vue`.
- `config.js` → `src/config.js` с `import.meta.env.VITE_API_BASE_URL` (+ `config.spec.js`); импорты `APP_CONFIG` во всех компонентах; `.env` в `.gitignore`.
- `src/sharedFormat.spec.js` — страж от локальных форматтеров в `.vue`.
- Документы: `CLAUDE.md`, `.ai-docs/workflow/styleguide.md`, `.ai-docs/project/project-summary.md`.

## Шаги
- [x] Красный тест: `toInputDate` для даты платежа `"2026-10-20T00:00:00"` возвращает `2026-10-20` (баг модалки на главной — UTC-сдвиг `toISOString()`)
- [x] format.js + тесты
- [x] Замена копий
- [x] env для API
- [x] Ручная проверка
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [x] PR

## Тесты
- `src/utils/format.spec.js` (Vitest): `formatCurrency` (копейки, отрицательные, null/undefined → 0), `formatSignedCurrency` (знак), `formatShortAmount` (к/М), `parseDate` (строка только с датой — локальная полночь), `formatDate`/`formatShortDate`/`formatDateTime`/`formatLongDate` (пустые значения → заглушка), `toInputDate` (Date и строка API без сдвига на день; тесты запускаются с `TZ` с положительным смещением, чтобы сдвиг воспроизводился независимо от машины).
- `src/config.spec.js`: дефолтный адрес без env и адрес из `VITE_API_BASE_URL`.
- `heatmap.js`: `formatHeatCurrency` удалён (заменён `formatSignedCurrency`), его тесты перенесены в `format.spec.js`.
- Статический тест: в `.vue`-файлах нет локальных `formatCurrency`/`formatDate`/`Intl.NumberFormat`/`toISOString`.

## Ручная проверка
Preview `backend` + `frontend`: главная (тепловая карта, модалка ячейки — суммы; модалка правки платежа — сумма и дата совпадают с датой платежа), «Операции» (дата+время, суммы), «Дашборд» (суммы, даты, оси графика, диапазоны по умолчанию), «Источники» (даты, модалка платежей источника — суммы), «Счета», «Теги» (даты создания). Консоль браузера без ошибок.

## Риски и открытые вопросы
- Небольшие визуальные изменения форматов: в модалке платежей источника сумма теперь всегда с 2 знаками копеек и в общем формате валюты (`-1 795,00 ₽` вместо `-1 795 ₽`).

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: «стек веток» — ветка создана от `refactor/0020-frontend-single-bootstrap` (PR #23 не смёржен), а не от `main` (отступление от п. 1.2 регламента); PR открывается в базу `refactor/0020-frontend-single-bootstrap`, мёржить после #23.
- 2026-10-20: в задачу добавлен отложенный баг — дата на день раньше в модалке правки платежа на главной (`toISOString()` в UTC). Тот же сдвиг найден в `Dashboard.vue` (`toInputDate` для диапазона по умолчанию) — исправляется заодно общей `toInputDate`.
- 2026-10-20: `config.js` перенесён в `src/config.js` (аудит: «лежит вне `src/`»); имя `APP_CONFIG.apiBaseUrl` сохранено, чтобы не менять вызовы axios.
- 2026-10-20: опциональный модуль загрузки справочников (`getTree`, теги, счета) не делается — требовал согласования и близок к API-слою, который вне объёма; см. «Вне объёма».
- 2026-10-20: `.env.example` из постановки не добавлен: хук pre-pr-check блокирует любые `.env.*` в диффе (ложное срабатывание), а переименование файла ради проверки — обход хука. Переменная описана в `CLAUDE.md`, `project-summary.md` и комментарии `src/config.js` (переопределять в `.env.local`); `.env` добавлен в `.gitignore`. Исправление хука — отдельной задачей (см. «Вне объёма»).
- 2026-10-20: `formatHeatCurrency` удалён из `heatmap.js` (а не оставлен алиасом) — единое имя `formatSignedCurrency` во всех местах.
- 2026-10-20: поле «Дата» в модалке правки платежа (disabled text input) оставлено в формате `YYYY-MM-DD` через `toInputDate` — исправлен только сдвиг, без изменения внешнего вида.
- 2026-10-20: Options API-компоненты (`PaymentList`, `PaymentSourcesList`, `PaymentCategoryRow`) подключают общие форматтеры через `methods`, перевод на `<script setup>` — задача 0022.
- 2026-10-20: `formatDate` принимает заглушку для пустого значения вторым параметром (по умолчанию `-`, как было на страницах-списках).

## Вне объёма (найдено попутно)
- **Ложное срабатывание хука pre-pr-check:** правило `(^|/).env(.|$)` блокирует и безопасный шаблон `.env.example`, поэтому он не добавлен (переименование файла ради обхода проверки — это обход хука, регламент §4 запрещает). Предложение: отдельная `chore`-задача — разрешить в хуке `.env.example` и затем добавить его во frontend.
- Модуль загрузки справочников (`CategoriesApi/getTree`, теги, счета, источники) — запросы по-прежнему дублируются в `Payments.vue`, `PaymentsTable.vue`, `PaymentSources.vue`, `Categories.vue`, `CategoryTreeSelect.vue`; можно сделать отдельной задачей вместе с API-слоем.
- `PaymentList.vue`: `payment.paymentSource.description` без `?.` — упадёт рендер, если у платежа нет источника (рядом уже стоит `paymentSource?.paymentSourceName`).
- Options API в `PaymentCategoryRow.vue`, `PaymentList.vue`, `PaymentSourcesList.vue` и `alert()` — оставлены задаче 0022.

## Итог
- Сделано: `src/utils/format.js` (суммы, сокращённые суммы, даты, дата для input без UTC-сдвига) + 21 тест; удалены все локальные копии форматтеров (9 файлов), `heatmap.formatHeatCurrency` → `formatSignedCurrency`; страж `sharedFormat.spec.js`. Исправлен сдвиг даты на день раньше в модалке правки платежа на главной (и тот же сдвиг диапазона по умолчанию на дашборде). Адрес API — `VITE_API_BASE_URL` с дефолтом `http://localhost:5000`, `config.js` перенесён в `src/`. Актуализированы `CLAUDE.md`, `styleguide.md`, `project-summary.md`.
- Проверено: `npm run test` — 7 файлов, 57 тестов; `npm run build` — без ошибок; сборка с `VITE_API_BASE_URL` подставляет адрес в бандл; backend `dotnet build`/`dotnet test` (дифф `main...HEAD` стека) — 0 ошибок/предупреждений, 169 тестов. Ручная проверка в preview — все страницы (см. «Ручная проверка»): модалка правки платежа показывает дату `2026-09-30` для платежа «30 сентября» (до исправления — `2026-09-29` в UTC+5); консоль браузера после перезагрузки без ошибок, в логах backend ошибок нет. Данные dev-базы не менялись (модалка закрыта без сохранения).
- Отложено: `.env.example` и модуль справочников — см. «Вне объёма».
