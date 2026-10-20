# План: [0016] Транзакции, N+1 и доменные правила вне Infrastructure

> Статус: `запланирована`. При старте сверить план с текущим кодом (номера строк из аудита могли сместиться) и обновить.

## Затрагиваемые части
- `Common.Infrastructure/IDbConnectionFactory.cs`, `Infrastructure/DbConnectionFactory.cs`
- `Infrastructure/Payments/PaymentRepository.cs`, `PaymentDto.cs`
- `Infrastructure/Tags/TagRepository.cs`
- `Infrastructure/Categories/CategoryRepository.cs`
- `Application/PaymentsImport/PaymentsImportService.cs`
- `Domain/Payment.cs`

## Шаги
- [ ] Транзакционная абстракция
- [ ] Транзакции в репозиториях и импорте
- [ ] Дерево категорий
- [ ] Знак суммы в Domain
- [ ] NOLOCK
- [ ] Логи и CancellationToken в импорте
- [ ] Тесты и ручная проверка
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
Уточнить при старте задачи (регламент §3).

## Ручная проверка
Сценарии из критериев приемки — в preview `backend` + `frontend` / на dev-базе.

## Риски и открытые вопросы
- Дизайн unit-of-work затрагивает все репозитории. Согласовать подход с пользователем до реализации.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
