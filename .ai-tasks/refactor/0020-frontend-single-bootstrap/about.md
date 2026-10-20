# [0020] Один Bootstrap 5.3 из npm

- **Ветка:** `refactor/0020-frontend-single-bootstrap`
- **Репозитории:** frontend
- **Создана:** 2026-10-20
- **Источник:** `.ai-docs/project/architecture-audit.md`, §3

## Контекст
`index.html` загружает с CDN CSS и JS Bootstrap 5.0.2, а в `package.json` указан `bootstrap ^5.3.8`, и стайлгайд требует 5.3. Модалки создаются через две разные копии: `Categories`/`PaymentSources` используют `import { Modal }` (npm), а `Payments`/`PaymentsTable` — глобальный `bootstrap.Modal` (CDN).

## Постановка
- Убрать CDN-подключения из `index.html`.
- Импортировать `bootstrap/dist/css/bootstrap.min.css` (и при необходимости bundle JS) в `main.js`.
- Везде использовать `import { Modal } from 'bootstrap'`.

## Вне объёма
- Прочие правки стайлгайда (0022).

## Критерии приемки
- [ ] В `index.html` нет CDN Bootstrap
- [ ] Все модалки (главная, Операции, Категории, Источники, Счета, Теги) открываются и закрываются
- [ ] Визуальных регрессий нет (сверить страницы до/после)
- [ ] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
- [ ] Все тесты зелёные
