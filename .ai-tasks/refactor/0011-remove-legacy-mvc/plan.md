# План: [0011] Удалить legacy MVC-фронтенд, оставить только REST API

> Статус: `запланирована`. При старте сверить план с текущим кодом (номера строк из аудита могли сместиться) и обновить.

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
Уточнить при старте задачи (регламент §3).

## Ручная проверка
Сценарии из критериев приемки — в preview `backend` + `frontend` / на dev-базе.

## Риски и открытые вопросы
- Swagger UI берёт статику из пакета, `UseStaticFiles` ему не нужен. Проверить вручную.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
