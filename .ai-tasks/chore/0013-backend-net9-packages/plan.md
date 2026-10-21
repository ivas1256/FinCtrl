# План: [0013] Перевести backend на net9 и Microsoft.Data.SqlClient

> Статус: `на ревью`.

## Сверка с кодом (2026-10-20)
- TFM: Website и оба тестовых проекта — `net9.0`; Domain/Application/Infrastructure — `net7.0`; Common/Common.Infrastructure — `net6.0`.
- `System.Data.SqlClient` 4.8.6 — в `Common.Infrastructure` и `Infrastructure`; используется в `IDbConnectionFactory` и `DbConnectionFactory`.
- Пакеты: `Microsoft.Extensions.*` — 8.0.0/8.0.1 вперемешку; `Microsoft.Bcl.TimeProvider` 8.0.1 в Application (на net8+ `TimeProvider` встроен в BCL); `Microsoft.Extensions.TimeProvider.Testing` 8.10.0 в тестах.
- Строка подключения dev уже содержит `TrustServerCertificate=True` — правка `appsettings.Development.json` не нужна.
- Базовая сборка: 0 ошибок, 17 предупреждений nullable (`DashboardSummary`, `MainStatisticGroupItem`, `Infrastructure/Payments/PaymentDto`, `PaymentEditDto` — CS8632 из-за выключенного Nullable в Website).

## Затрагиваемые части
- все `*.csproj` из .sln
- `Common.Infrastructure/IDbConnectionFactory.cs`, `Infrastructure/DbConnectionFactory.cs`
- файлы Website с предупреждениями после включения Nullable/ImplicitUsings, `Startup.cs`
- `Domain/Dashboard/DashboardSummary.cs`, `Domain/Statistic/MainStatisticGroupItem.cs`, `Infrastructure/Payments/PaymentDto.cs`

## Шаги
- [x] TFM: все проекты на `net9.0`
- [x] SqlClient: `System.Data.SqlClient` → `Microsoft.Data.SqlClient`
- [x] Версии пакетов: `Microsoft.Extensions.*` → 9.0.4, `TimeProvider.Testing` → 9.0.0, убрать `Microsoft.Bcl.TimeProvider`
- [x] Nullable/ImplicitUsings в Website + исправление предупреждений (включая 17 давних)
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [x] PR

## Тесты
Поведение не меняется — новые юнит-тесты не пишутся; критерий — существующие тесты (`dotnet test FinCtrl.Backend.sln`) зелёные на net9 (22 теста: Domain 8, Application 14). Подключение к БД через `Microsoft.Data.SqlClient` юнит-тестами не покрывается (регламент §3) — проверено вручную. Мелкие изменения `EditPayment` (404, `?.`) проверены вручную через API: тестового проекта для Website нет (см. «Вне объёма»).

## Ручная проверка
- `dotnet build FinCtrl.Backend.sln` — 0 ошибок, 0 предупреждений; `dotnet test` — 22/22.
- API (preview `backend`): `GET` всех ресурсов — 200 (`AccountsApi`, `TagsApi`, `CategoriesApi/getAll|getTree`, `PaymentSourcesApi`, `PaymentSourcesApi/list`, `PaymentsApi?periodType=1`, `PaymentsApi/list`, `DashboardApi/summary|chart`), `/swagger/index.html` и `/swagger/v1/swagger.json` — 200.
- Запись: `PUT api/PaymentsApi/7929` с `description`/`excludeFromStatsStr`/`categoryId`/`accountId`/`tagIds` = null — 200, описание сохранилось; несуществующий платёж — 404; CRUD тега (create/update/delete) — 200; `POST api/TagsApi` с `tagName: null` — 400 с прежним текстом «Название тега обязательно» (неявный `[Required]` не сработал); `PUT api/AccountsApi/1` — 200.
- Импорт: `POST api/PaymentsApi/upload` CSV с одной строкой — 200, создан платёж и источник; повторная загрузка — дубль не создан.
- Данные dev-базы возвращены: платёж 7929 (`PaymentDescription`, `LastUpdatedAt` = NULL), счёт 1 (`DisplayName`, `LastUpdatedAt`), удалены тестовые платёж/источник/тег, identity `Payments`/`PaymentSources`/`Tags` пересеяны на исходные значения.
- SPA (preview `frontend`): Главная, Дашборд, Операции, Источники, Категории, Счета, Теги — данные загружаются, ошибок в консоли браузера и в логах backend нет.

## Риски и открытые вопросы
- `Microsoft.Data.SqlClient` по умолчанию включает `Encrypt=True`. Без `TrustServerCertificate=True` подключение к локальному серверу не пройдёт (в dev-строке есть).

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: отступление от п.1.2 регламента — по выбору пользователя «стек веток»: ветка создана от `chore/0012-backend-code-cleanup` (PR #15, не смёржен), PR открывается в неё же и мёржится после #15. Дифф `main...HEAD` для хука включает изменения предыдущих задач стека — это ожидаемо.
- 2026-10-20: `Microsoft.Data.SqlClient` — 6.1.1 (стабильная ветка 6.x). Dapper и SqlClient оставлены только в `Common.Infrastructure` (Infrastructure получает транзитивно), чтобы версии не расходились; `Microsoft.Extensions.DependencyInjection` из Infrastructure убран — нужны только Abstractions. `Microsoft.Extensions.*` — 9.0.4 (линейка net9).
- 2026-10-20: включение Nullable в Website по умолчанию делает non-nullable ссылочные поля DTO неявно `[Required]` (400 в формате ProblemDetails вместо прежних ответов). Чтобы не менять контракт API, выставлено `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`; обязательность — только явными атрибутами (как в `EditPaymentSourceRequest`). Обязательные строки DTO — `= string.Empty`, необязательные — `string?`.
- 2026-10-20: в `EditPayment` для чистоты nullable добавлен `404` при ненайденном платеже и `PaymentSource?.` (раньше NRE → 500). Это минимальные изменения ради предупреждений; NRE при пустом описании из бэклога гайда уже не воспроизводится (описание — `string?`).
- 2026-10-20: в `BuildTree` убрана пустая вставка `Periods.Add(key, null)` перед присваиванием — та же семантика, без null в non-nullable словаре.
- 2026-10-20: при ручной проверке страницы SPA висели на «Загрузка...»: запросы ждали ~25 с с `RESOURCE_SEMAPHORE` в `sys.dm_exec_requests` (память SQL Server на машине урезана до ~116 МБ, семафор грантов 8 МБ). Проверено сравнением на отдельном запуске: воспроизводится и с `MultipleActiveResultSets=True`, и без — это окружение, не драйвер; правка строки подключения откачена. После освобождения памяти страницы грузятся нормально.

## Вне объёма (найдено попутно)
- Нет тестового проекта `FinCtrl.Website.Tests` — логику контроллеров (`EditPayment`, `BuildTree`) нечем покрыть юнит-тестами; уместно в 0018 (тонкие контроллеры) или 0023.
- `appsettings.Production.json` (в репозитории) содержит `MultipleActiveResultSets=True`; MARS в коде не используется — можно убрать вручную. Файл не трогал: хук запрещает `appsettings.Production*` в диффе.
- Dev-окружение: при нехватке памяти у SQL Server параллельные запросы SPA ждут memory grant до таймаута — стоит задать `min server memory` у локального инстанса (настройка сервера, не код).

## Итог
Все проекты `.sln` переведены на `net9.0`, `System.Data.SqlClient` заменён на `Microsoft.Data.SqlClient` 6.1.1, версии пакетов выровнены, в Website включены `Nullable`/`ImplicitUsings`; закрыты 17 давних и все новые nullable-предупреждения — сборка backend без предупреждений. Контракт API сохранён (неявный `[Required]` отключён). Отложено: см. «Вне объёма».
