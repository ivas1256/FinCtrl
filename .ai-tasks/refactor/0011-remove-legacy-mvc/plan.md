# План: [0011] Удалить legacy MVC-фронтенд, оставить только REST API

> Статус: `в работе`. План сверен с кодом 2026-10-20.

## Затрагиваемые части
- `FinCtrl.Website/Controllers/*Controller.cs` (не Api)
- `FinCtrl.Website/Views/`, `Models/`, `wwwroot/`, `jsconfig.json`
- `FinCtrl.Website/Startup.cs`, `FinCtrl.Website.csproj`
- `Application/Payments/IPaymentService.cs`, `PaymentService.cs`; `Infrastructure/Payments/*`
- `Common/Pagination.cs`
- `Website/RequestDto/CategoryDto.cs`

## Шаги
- [ ] Удалить контроллеры, Views, Models, wwwroot
- [ ] Startup: только API
- [ ] Удалить осиротевшие члены (по ошибкам компиляции и `git grep`)
- [ ] Удалить пакет CodeGeneration.Design
- [ ] Сборка, ручной прогон SPA
- [ ] Документация
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
Задача удаляет код без изменения поведения REST API, новой бизнес-логики нет. Единственная правка логики — `PaymentSourceService.Update(params)` убран из интерфейса и встроен в `Edit`; поведение `Edit` уже покрыто тестами `PaymentSourceServiceTests.Edit_*` (в т.ч. проставление `LastUpdatedAt`), они остаются зелёными. Новые юнит-тесты не нужны; проверка — сборка, `dotnet test` и ручной смоук SPA/API.

## Ручная проверка
Сценарии из критериев приемки — в preview `backend` + `frontend` / на dev-базе.

## Риски и открытые вопросы
- Swagger UI берёт статику из пакета, `UseStaticFiles` ему не нужен. Проверить вручную.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: отступление от п. 1.2 регламента по решению пользователя («стек веток»): ветка задачи создана не от `main`, а от `refactor/0010-remove-dead-projects` (PR #13, стек #9→…→#13 не смёржен); PR — в `refactor/0010-remove-dead-projects`, мёржить после #13. Дифф `main...HEAD`, который смотрит хук, включает изменения предыдущих задач стека — это ожидаемо.
- 2026-10-20: осиротевшими после удаления MVC признаны: `IPaymentService`/`IPaymentRepository.GetAll(Pagination)` (вместе с ним уходит вопрос «GetAll без фильтра IsDeleted» из «Вне объёма» 0008), `IPaymentService`/`IPaymentRepository.Delete` (мягкое удаление платежа — в SPA удаления платежей нет; понадобится — вернуть вместе с API), класс `Pagination`, `DateTimeExtensions.TryParseExact`, `IPaymentSourceService.Update(params)` (после удаления MVC вызывался только из `Edit` — встроен в него), `CategoryDto.ToModel`/`IsOnlyIncomeStr`/`ParentCategoryId`/`OrderIndex` (в ответах API последние два всегда были null/0, фронтенд их у категорий платежа не читает).
- 2026-10-20: `PaymentEditDto.ExcludeFromStatsStr` оставлен — его шлёт SPA (`Payments.vue`) и читает `PaymentsApiController.EditPayment`. Члены, которые и до задачи не использовались ни MVC, ни API (`IPaymentService.Exists`, `IPaymentSourceService.Delete`), не тронуты — записаны во «Вне объёма».
- 2026-10-20: `.config/dotnet-tools.json` (локальный инструмент `dotnet-ef`) не относится к MVC — не тронут, записан во «Вне объёма».

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
