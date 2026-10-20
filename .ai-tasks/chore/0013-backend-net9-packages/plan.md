# План: [0013] Перевести backend на net9 и Microsoft.Data.SqlClient

> Статус: `в работе`.

## Сверка с кодом (2026-10-20)
- TFM: Website и оба тестовых проекта — `net9.0`; Domain/Application/Infrastructure — `net7.0`; Common/Common.Infrastructure — `net6.0`.
- `System.Data.SqlClient` 4.8.6 — в `Common.Infrastructure` и `Infrastructure`; используется в `IDbConnectionFactory` и `DbConnectionFactory`.
- Пакеты: `Microsoft.Extensions.*` — 8.0.0/8.0.1 вперемешку; `Microsoft.Bcl.TimeProvider` 8.0.1 в Application (на net8+ `TimeProvider` встроен в BCL); `Microsoft.Extensions.TimeProvider.Testing` 8.10.0 в тестах.
- Строка подключения dev уже содержит `TrustServerCertificate=True` — правка `appsettings.Development.json` не нужна.
- Базовая сборка: 0 ошибок, 17 предупреждений nullable (`DashboardSummary`, `MainStatisticGroupItem`, `Infrastructure/Payments/PaymentDto`, `PaymentEditDto` — CS8632 из-за выключенного Nullable в Website).

## Затрагиваемые части
- все `*.csproj` из .sln
- `Common.Infrastructure/IDbConnectionFactory.cs`, `Infrastructure/DbConnectionFactory.cs`
- файлы Website с предупреждениями после включения Nullable/ImplicitUsings
- `Domain/Dashboard/DashboardSummary.cs`, `Domain/Statistic/MainStatisticGroupItem.cs`, `Infrastructure/Payments/PaymentDto.cs`

## Шаги
- [ ] TFM: все проекты на `net9.0`
- [ ] SqlClient: `System.Data.SqlClient` → `Microsoft.Data.SqlClient`
- [ ] Версии пакетов: `Microsoft.Extensions.*` → 9.0.x, убрать `Microsoft.Bcl.TimeProvider`
- [ ] Nullable/ImplicitUsings в Website + исправление предупреждений (включая 17 давних)
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
Поведение не меняется — новые юнит-тесты не пишутся; критерий — существующие тесты (`dotnet test FinCtrl.Backend.sln`) зелёные на net9. Подключение к БД через `Microsoft.Data.SqlClient` юнит-тестами не покрывается (регламент §3) — проверяется вручную.

## Ручная проверка
- preview `backend` + `frontend`: смоук всех страниц SPA, отсутствие ошибок в консоли и логах backend;
- swagger `/swagger`;
- чтение и запись через API (правка с возвратом исходных значений), импорт файла через `POST api/PaymentsApi/upload` (с откатом тестовых данных на dev-базе).

## Риски и открытые вопросы
- `Microsoft.Data.SqlClient` по умолчанию включает `Encrypt=True`. Без `TrustServerCertificate=True` dev-подключение может не пройти (в dev-строке уже есть).

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: отступление от п.1.2 регламента — по выбору пользователя «стек веток»: ветка создана от `chore/0012-backend-code-cleanup` (PR #15, не смёржен), PR открывается в неё же и мёржится после #15. Дифф `main...HEAD` для хука включает изменения предыдущих задач стека — это ожидаемо.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
