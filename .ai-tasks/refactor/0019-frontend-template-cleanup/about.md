# [0019] Удалить остатки шаблона Vite и отладочный код

- **Ветка:** `refactor/0019-frontend-template-cleanup`
- **Репозитории:** frontend
- **Создана:** 2026-10-20
- **Источник:** `.ai-docs/project/architecture-audit.md`, §3

## Контекст
Во frontend остались файлы шаблона Vite, мёртвые импорты, неиспользуемые утилиты и отладочные `console.log`.

## Постановка
- Удалить `src/components/HelloWorld.vue`, `TheWelcome.vue`, `WelcomeItem.vue`, `src/components/icons/*`, `src/assets/logo.svg`, `src/pages/paymentsTable/Style.css`, `src/utils/sortUtils.js`.
- Убрать мёртвые импорты в `src/App.vue:2-4`.
- Удалить `console.log` в `PaymentsTable.vue` и debug-`watch` (`:576-578`) вместе с импортом `watch`.
- Заменить шаблонный `README.md` кратким описанием (команды, ссылка на CLAUDE.md); `index.html` → `lang="ru"`.

## Вне объёма
- `assets/base.css` — меняет внешний вид, решение в 0022.
- Bootstrap (0020).

## Критерии приемки
- [ ] Перечисленных файлов нет, `git grep` не находит импортов на них
- [ ] Нет `console.log` в `src/`
- [ ] `npm run build` проходит; приложение выглядит и работает как раньше
- [ ] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
- [ ] Все тесты зелёные
