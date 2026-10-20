# FinCtrl.Frontend

Frontend приложения FinCtrl — Vue 3 (`<script setup>`) + Vite, Bootstrap 5, axios, Chart.js.

Общий контекст проекта, архитектура и регламент работы — в [`CLAUDE.md`](../../CLAUDE.md) в корне репозитория.

## Команды

```sh
npm install      # установить зависимости
npm run dev      # dev-сервер Vite на http://localhost:5173
npm run build    # production-сборка в dist/
npm run test     # юнит-тесты Vitest (jsdom), файлы *.spec.js рядом с кодом
```

Адрес backend API задаётся в `config.js` (`APP_CONFIG.apiBaseUrl`, по умолчанию `http://localhost:5000`).

## Структура

- `index.js` — роуты приложения;
- `src/App.vue` — каркас и боковое меню;
- `src/pages/<фича>/` — страницы;
- `src/components/` — общие компоненты;
- `src/utils/` — чистые функции (с тестами `*.spec.js`).
