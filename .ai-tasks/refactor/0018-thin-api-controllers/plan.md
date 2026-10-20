# План: [0018] Тонкие API-контроллеры и DTO-контракт

> Статус: `запланирована`. При старте сверить план с текущим кодом (номера строк из аудита могли сместиться) и обновить.

## Затрагиваемые части
- `Website/Controllers/PaymentsApi/*`, `DashboardApi/*`, `CategoriesApi/*`, `RequestDto/*`
- `Application/Statistic/*`, `Dashboard/*`, `Payments/*`
- `Domain/Payment.cs`, `Category.cs`, `Common/DateRange.cs`
- Vue-страницы, отправляющие `ExcludeFromStatsStr`/`IsOnlyIncome`

## Шаги
- [ ] Тесты на текущее поведение (характеризационные)
- [ ] Перенос логики Payments/Statistic
- [ ] Dashboard
- [ ] Правка платежа в сервис
- [ ] DTO/маппинг/Response
- [ ] Константы и DateRange
- [ ] bool-контракт + frontend
- [ ] record → class
- [ ] Ручная проверка, документация
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
Уточнить при старте задачи (регламент §3).

## Ручная проверка
Сценарии из критериев приемки — в preview `backend` + `frontend` / на dev-базе.

## Риски и открытые вопросы
- Самая крупная задача. При старте предложить пользователю разбить её на 2–3 PR: Payments/Statistic, Dashboard, DTO-контракт.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
