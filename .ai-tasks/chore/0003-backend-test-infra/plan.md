# План: [0003] Тестовая инфраструктура backend

> Статус: `запланирована`. При старте сверить план с текущим кодом (номера строк из аудита могли сместиться) и обновить.

## Затрагиваемые части
- `FinCtrl/tests/FinCtrl.Application.Tests/*`
- `FinCtrl.Backend.sln`
- `src/main/FinCtrl.Application/*.csproj`, `ServiceCollectionExtensions.cs`
- Domain: `Account.cs`, `Category.cs`, `Payment.cs`, `PaymentSource.cs`, `Tag.cs`
- `Application/Dashboard/DashboardService.cs`
- `Infrastructure/Categories/CategoryRepository.cs` (`CreatedAt`)

## Шаги
- [ ] Создать тестовый проект, подключить в .sln
- [ ] InternalsVisibleTo
- [ ] TimeProvider в DI, убрать DateTime.Now/Today
- [ ] Первые тесты
- [ ] Обновить CLAUDE.md (раздел «Команды»)
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
Уточнить при старте задачи (регламент §3).

## Ручная проверка
Сценарии из критериев приемки — в preview `backend` + `frontend` / на dev-базе.

## Риски и открытые вопросы
- Приложения на net7 не имеют встроенного `TimeProvider`. Нужен пакет `Microsoft.Bcl.TimeProvider` или сначала задача 0013. Проще всего взять пакет.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
