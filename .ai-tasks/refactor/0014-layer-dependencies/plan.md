# План: [0014] Исправить направление зависимостей между слоями

> Статус: `запланирована`. При старте сверить план с текущим кодом (номера строк из аудита могли сместиться) и обновить.

## Затрагиваемые части
- `Application/*.csproj`, `Infrastructure/*.csproj`
- `Infrastructure/*/I*Repository.cs` → `Application/*/`
- `Application/PaymentsImport/FileFormats/*` → `Infrastructure/PaymentsImport/`
- DI: `AddApplication`, `AddInfrastructure`

## Шаги
- [ ] Перенос интерфейсов
- [ ] Разворот ссылок
- [ ] Перенос FileFormats
- [ ] Сборка, тесты
- [ ] Импорт xlsx/csv вручную
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
Уточнить при старте задачи (регламент §3).

## Ручная проверка
Сценарии из критериев приемки — в preview `backend` + `frontend` / на dev-базе.

## Риски и открытые вопросы
- `PaymentDto` из Infrastructure используется в контракте репозитория. Решить, что станет read-моделью в Application, а что останется внутренним маппингом.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
