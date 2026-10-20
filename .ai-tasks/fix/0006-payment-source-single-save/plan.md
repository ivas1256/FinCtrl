# План: [0006] Не работает одиночное сохранение источника

> Статус: `в работе`. План сверен с кодом ветки `fix/0005-quarter-grouping` (2026-10-20).

## Сверка с текущим кодом
- `PaymentSources.vue` (`saveSingleItem`, ~стр. 394) уже шлёт `PUT /api/PaymentSourcesApi/{id}` с `paymentSourceName`/`categoryId`/`description` в теле, без `paymentSourceId` — это и есть корректный по гайду запрос (id из роута). Проблема только на backend.
- `PaymentSourcesApiController.EditSources` — `[HttpPut]` без шаблона, логика правки (поиск категории + `PaymentSource.CreateFromExisting` + `Update`) живёт в контроллере.

## Затрагиваемые части
- `FinCtrl/src/main/FinCtrl.Application/PaymentSources/IPaymentSourceService.cs`, `PaymentSourceService.cs` — новый метод `Edit` (сценарий правки).
- `FinCtrl/FinCtrl.Website/Controllers/PaymentSourceesApi/PaymentSourcesApiController.cs` — новый action `PUT api/PaymentSourcesApi/{paymentSourceId}`, старый `PUT api/PaymentSourcesApi` переведён на `Edit`.
- `FinCtrl/tests/FinCtrl.Application.Tests/PaymentSources/PaymentSourceServiceTests.cs` — тесты.
- Frontend: `PaymentSources.vue` — правок не требует (см. журнал решений).

## Шаги
- [ ] Воспроизвести в preview (PUT `/api/PaymentSourcesApi/{id}` → 404/405)
- [ ] Красный тест на `PaymentSourceService.Edit`
- [ ] Перенести логику правки в `PaymentSourceService.Edit` (зелёный тест)
- [ ] Маршрут `PUT api/PaymentSourcesApi/{paymentSourceId}`, старый PUT — через `Edit`
- [ ] Правка frontend (проверить запрос; при необходимости поправить)
- [ ] Ручная проверка одиночного и массового сохранения
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
`PaymentSourceServiceTests` (xUnit + NSubstitute + `FakeTimeProvider`):
- `Edit` с категорией — категория подтягивается через `ICategoryService.GetById`, в репозиторий уходит источник с id, именем, описанием, категорией и `LastUpdatedAt` = текущее время провайдера;
- `Edit` без категории (`categoryId == null`) — категория не запрашивается, у источника `PaymentSourceCategory == null`.

## Ручная проверка
Preview `backend` + `frontend`, страница «Источники»: сменить категорию/описание у источника → 💾 → перезагрузка → изменения на месте; сменить у двух источников → «Сохранить все» → перезагрузка → изменения на месте. Нет ошибок в консоли браузера и логах backend. После проверки вернуть исходные значения на dev-базе.

## Риски и открытые вопросы
- Тело одиночного PUT может содержать `paymentSourceId`, расходящийся с роутом, — используется значение из роута (по гайду).

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: «стек веток» по решению пользователя — ветка ответвлена от `fix/0005-quarter-grouping` (PR #9, ещё не смёржен), PR создаётся в `fix/0005-quarter-grouping`, мёржить после PR #9. Отступление от п. 1.2 регламента (база — `main`). Хук pre-pr-check сравнивает `main...HEAD`, поэтому в его проверке будут и изменения 0005 — это ожидаемо.
- 2026-10-20: frontend уже отправляет запрос в корректной форме (id в роуте, поля в теле) — менять его не нужно, баг чисто backend'овый. Пункт «Правка frontend» закрывается проверкой.
- 2026-10-20: логика правки (поиск категории, сборка сущности, обновление) перенесена из контроллера в `PaymentSourceService.Edit` — чтобы оба PUT использовали один сценарий без дублирования и чтобы его можно было покрыть юнит-тестом (тестового проекта для Website нет). Красный тест — тест на ещё не существующий `Edit` (не компилируется), затем реализация.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
