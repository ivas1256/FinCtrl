# План: [0013] Перевести backend на net9 и Microsoft.Data.SqlClient

> Статус: `запланирована`. При старте сверить план с текущим кодом (номера строк из аудита могли сместиться) и обновить.

## Затрагиваемые части
- все `*.csproj` из .sln
- `Common.Infrastructure/IDbConnectionFactory.cs`, `Infrastructure/DbConnectionFactory.cs`
- `appsettings.Development.json` (при необходимости `TrustServerCertificate=True`)

## Шаги
- [ ] TFM
- [ ] SqlClient
- [ ] Версии пакетов
- [ ] Nullable в Website + исправление предупреждений
- [ ] Сборка, тесты, ручная проверка
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
Уточнить при старте задачи (регламент §3).

## Ручная проверка
Сценарии из критериев приемки — в preview `backend` + `frontend` / на dev-базе.

## Риски и открытые вопросы
- `Microsoft.Data.SqlClient` по умолчанию включает `Encrypt=True`. Без `TrustServerCertificate=True` dev-подключение может не пройти.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
