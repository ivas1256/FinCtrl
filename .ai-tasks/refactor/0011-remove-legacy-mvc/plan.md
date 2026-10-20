# План: [0011] Удалить legacy MVC-фронтенд, оставить только REST API

> Статус: `на ревью` (PR https://github.com/ivas1256/FinCtrl/pull/14). План сверен с кодом 2026-10-20.

## Затрагиваемые части
- `FinCtrl.Website/Controllers/*Controller.cs` (не Api)
- `FinCtrl.Website/Views/`, `Models/`, `wwwroot/`, `jsconfig.json`
- `FinCtrl.Website/Startup.cs`, `FinCtrl.Website.csproj`
- `Application/Payments/IPaymentService.cs`, `PaymentService.cs`; `Infrastructure/Payments/*`
- `Common/Pagination.cs`
- `Website/RequestDto/CategoryDto.cs`

## Шаги
- [x] Удалить контроллеры, Views, Models, wwwroot
- [x] Startup: только API
- [x] Удалить осиротевшие члены (по ошибкам компиляции и `git grep`)
- [x] Удалить пакет CodeGeneration.Design
- [x] Сборка, ручной прогон SPA
- [x] Документация
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [x] PR

## Тесты
Задача удаляет код без изменения поведения REST API, новой бизнес-логики нет. Единственная правка логики — `PaymentSourceService.Update(params)` убран из интерфейса и встроен в `Edit`; поведение `Edit` уже покрыто тестами `PaymentSourceServiceTests.Edit_*` (в т.ч. проставление `LastUpdatedAt`), они остаются зелёными. Новые юнит-тесты не нужны; проверка — сборка, `dotnet test` и ручной смоук SPA/API.

## Ручная проверка
Сценарии из критериев приемки — в preview `backend` + `frontend` / на dev-базе. Выполнено 2026-10-20:
- `GET /`, `/index`, `/Payment/all`, `/Category/all`, `/payment-source`, `/css/site.css`, `DELETE /Payment/delete` → 404.
- `/swagger/index.html` и `/swagger/v1/swagger.json` → 200, в спецификации все 19 путей `api/*` (Accounts, Categories, Dashboard, PaymentSources, Payments, Tags).
- API: `CategoriesApi/getAll|getTree`, `TagsApi`, `AccountsApi`, `PaymentSourcesApi[/list]`, `PaymentsApi` (дерево), `PaymentsApi/list` с периодом, `DashboardApi/summary` → 200; у `category`/`effectiveCategory` в `PaymentsApi/list` теперь только `categoryId`/`categoryName`/`isOnlyIncome`.
- SPA: Главная, Дашборд, Операции, Источники, Категории, Счета, Теги открываются с данными, ошибок в консоли браузера и логах backend нет.
- Загрузка файла: `POST api/PaymentsApi/upload` (тот же вызов, что делает кнопка «Загрузить файл») с тестовым CSV → 200, создан платёж и источник `ZZZ_TEST_0011_SOURCE`, виден через `PaymentsApi/by-source`. Тестовые строки удалены, identity `Payments`/`PaymentSources` возвращены к 7948/1384.
- `PUT api/PaymentSourcesApi/1384` (через изменённый `PaymentSourceService.Edit`) → 204, `LastUpdatedAt` проставлен; исходные значения строки восстановлены.
- `GET api/PaymentsApi/list` без периода → 500 «too many parameters» — известная грабля `GetTagsForPayments` (см. 0005), к задаче не относится, SPA всегда передаёт период.

## Риски и открытые вопросы
- Swagger UI берёт статику из пакета, `UseStaticFiles` ему не нужен. Проверить вручную.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: отступление от п. 1.2 регламента по решению пользователя («стек веток»): ветка задачи создана не от `main`, а от `refactor/0010-remove-dead-projects` (PR #13, стек #9→…→#13 не смёржен); PR — в `refactor/0010-remove-dead-projects`, мёржить после #13. Дифф `main...HEAD`, который смотрит хук, включает изменения предыдущих задач стека — это ожидаемо.
- 2026-10-20: осиротевшими после удаления MVC признаны: `IPaymentService`/`IPaymentRepository.GetAll(Pagination)` (вместе с ним уходит вопрос «GetAll без фильтра IsDeleted» из «Вне объёма» 0008), `IPaymentService`/`IPaymentRepository.Delete` (мягкое удаление платежа — в SPA удаления платежей нет; понадобится — вернуть вместе с API), класс `Pagination`, `DateTimeExtensions.TryParseExact`, `IPaymentSourceService.Update(params)` (после удаления MVC вызывался только из `Edit` — встроен в него), `CategoryDto.ToModel`/`IsOnlyIncomeStr`/`ParentCategoryId`/`OrderIndex` (в ответах API последние два всегда были null/0, фронтенд их у категорий платежа не читает).
- 2026-10-20: `PaymentEditDto.ExcludeFromStatsStr` оставлен — его шлёт SPA (`Payments.vue`) и читает `PaymentsApiController.EditPayment`. Члены, которые и до задачи не использовались ни MVC, ни API (`IPaymentService.Exists`, `IPaymentSourceService.Delete`), не тронуты — записаны во «Вне объёма».
- 2026-10-20: `.config/dotnet-tools.json` (локальный инструмент `dotnet-ef`) не относится к MVC — не тронут, записан во «Вне объёма».

## Вне объёма (найдено попутно)
- `IPaymentService`/`IPaymentRepository.Exists(sum, date)` и `IPaymentSourceService.Delete` не используются, но и до задачи не использовались ни MVC, ни API — кандидаты для 0012.
- `FinCtrl.Website/.config/dotnet-tools.json` (локальный `dotnet-ef` 9.0.4) — EF в проекте нет, кандидат для 0012.
- `.claude/hooks/pre-pr-check.mjs` исключает из проверок `wwwroot/lib/` (`VENDORED`) — после удаления `wwwroot` правило не нужно; не трогал (правка хука — отдельной задачей).
- `CategoryDto` (выходной DTO вложенной категории платежа) лежит в `RequestDto/`, хотя запросом не является — перенос/переименование вместе с прочими DTO в 0018.

## Итог
Сделано: из `FinCtrl.Website` удалены MVC-контроллеры, `Views/`, `Models/`, `wwwroot/`, `jsconfig.json`, пустые `<Folder>` и пакет `Microsoft.VisualStudio.Web.CodeGeneration.Design`; `Startup` переведён на `AddControllers`/`MapControllers` без `UseStaticFiles`. Удалён код, нужный только MVC (см. «Журнал решений»). Обновлены `CLAUDE.md`, `project-summary.md`, `backend-guide.md`. Поведение REST API не изменилось (кроме пропавших всегда пустых полей вложенной категории). Осталось: пункты «Вне объёма».
