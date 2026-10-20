# План: [0006] Не работает одиночное сохранение источника

> Статус: `на ревью`. План сверен с кодом ветки `fix/0005-quarter-grouping` (2026-10-20).

## Сверка с текущим кодом
- `PaymentSources.vue` (`saveSingleItem`, ~стр. 394) уже шлёт `PUT /api/PaymentSourcesApi/{id}` с `paymentSourceName`/`categoryId`/`description` в теле, без `paymentSourceId` — это и есть корректный по гайду запрос (id из роута). Проблема только на backend.
- `PaymentSourcesApiController.EditSources` — `[HttpPut]` без шаблона, логика правки (поиск категории + `PaymentSource.CreateFromExisting` + `Update`) живёт в контроллере.

## Затрагиваемые части
- `FinCtrl/src/main/FinCtrl.Application/PaymentSources/IPaymentSourceService.cs`, `PaymentSourceService.cs` — новый метод `Edit` (сценарий правки).
- `FinCtrl/FinCtrl.Website/Controllers/PaymentSourceesApi/PaymentSourcesApiController.cs` — новый action `PUT api/PaymentSourcesApi/{paymentSourceId}`, старый `PUT api/PaymentSourcesApi` переведён на `Edit`.
- `FinCtrl/tests/FinCtrl.Application.Tests/PaymentSources/PaymentSourceServiceTests.cs` — тесты.
- Frontend: `PaymentSources.vue` — правок не требует (см. журнал решений).

## Шаги
- [x] Воспроизвести в preview (PUT `/api/PaymentSourcesApi/{id}` → 404/405)
- [x] Красный тест на `PaymentSourceService.Edit`
- [x] Перенести логику правки в `PaymentSourceService.Edit` (зелёный тест)
- [x] Маршрут `PUT api/PaymentSourcesApi/{paymentSourceId}`, старый PUT — через `Edit`
- [x] Правка frontend (проверить запрос; при необходимости поправить)
- [x] Ручная проверка одиночного и массового сохранения
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [x] PR — https://github.com/ivas1256/FinCtrl/pull/10 (база `fix/0005-quarter-grouping`)

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

- 2026-10-20: в новом action тело — отдельный `EditPaymentSourceRequest` (без id, `[Required]` у имени), ответ `204 NoContent` по гайду; legacy `PUT` оставлен с прежним контрактом (`PaymentSourceDto`, `200`). Неиспользуемая зависимость `ICategoryService` и лишние using убраны из контроллера; добавлен `#nullable enable` (в Website nullable-контекст выключен, а хук требует отсутствия предупреждений в изменённых файлах — до правки там было CS8632 на `string? search`).

## Вне объёма (найдено попутно)
- Правка несуществующего `paymentSourceId` отвечает `204`/`200`, хотя ничего не обновлено; несуществующий `categoryId` молча снимает категорию. Нужны `NotFoundException`/валидация → задача 0017.
- В `FinCtrl.Website` выключены `Nullable`/`ImplicitUsings` (в отличие от остальных проектов) — включение и чистка предупреждений — кандидат в 0012.

## Ручная проверка (выполнена 2026-10-20)
- До фикса: `PUT /api/PaymentSourcesApi/1384` → `404`.
- После фикса, preview `backend` + `frontend`, страница «Источники»: описание источника 1384 → 💾 → `PUT .../1384` → `204`, подсветка изменений снята, после перезагрузки описание на месте.
- Массовое: описания у 1383 и 1382 → «Сохранить все» → два `PUT /api/PaymentSourcesApi` → `200`, «Успешно обновлено 2 источников!», после перезагрузки значения на месте.
- Через API: `PUT .../1384` с `categoryId: 18` → категория установлена; с `categoryId: null` → снята; без `paymentSourceName` → `400` (ProblemDetails).
- Ошибок в консоли браузера и логах backend нет. Тестовые значения на dev-базе возвращены (описание и категория `null`).

## Итог
- Добавлен `PUT api/PaymentSourcesApi/{paymentSourceId}`; логика правки — в `PaymentSourceService.Edit` (2 юнит-теста), старый массовый `PUT` работает через неё же.
- Frontend не менялся — запрос уже был корректным.
- `dotnet build`/`dotnet test` — 0 ошибок, 15/15 тестов зелёные.
- PR: https://github.com/ivas1256/FinCtrl/pull/10 (в `fix/0005-quarter-grouping`, мёржить после #9).
