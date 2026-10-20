# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

FinCtrl — личное приложение для учёта финансов: пользователь загружает выгрузку из банка (Excel/CSV), операции привязываются к источникам платежа / счетам / категориям / тегам, UI показывает сводные таблицы. Корневая папка — единый git-репозиторий (базовая ветка `main`) с двумя частями:

- `FinCtrl/` — backend, .NET 9 + MS SQL Server (Dapper, без ORM).
- `FinCtrl.Frontend/FinCtrl.Frontend/` — frontend, Vue 3 + Vite.

Подробный контекст проекта — в `.ai-docs/`, сгруппирован по двум папкам:

- `.ai-docs/project/` — описание проекта и документация: `project-summary.md` (домен, API, известные грабли, история доработок), `architecture-audit.md` (аудит соответствия регламенту, бэклог задач 0003–0022 и порядок их выполнения), `general architecture.md` (исходное описание идеи и домена), `payments-table-heatmap.md` (спецификация тепловой карты на главной). При существенных изменениях актуализировать `project-summary.md`.
- `.ai-docs/workflow/` — регламенты работы: `task-workflow.md` (регламент работы над задачей, см. ниже), `styleguide.md` (UI-конвенции — следовать при создании новых страниц/компонентов), `backend-guide.md` (код-стайл и архитектура backend — следовать при любых правках в `FinCtrl/`).

## Регламент работы над задачей

**Обязателен для любой задачи, меняющей код** (ветка, папка задачи в `.ai-tasks/`, тесты, сборка, PR, журнал `.ai-tasks/tasks.md`). Полный текст:

@.ai-docs/workflow/task-workflow.md

## Команды

Backend (из `FinCtrl/`):
```bash
dotnet build FinCtrl.Backend.sln
dotnet test FinCtrl.Backend.sln   # xUnit-тесты из tests/
dotnet run --project FinCtrl.Website/FinCtrl.Website.csproj --urls http://localhost:5000
```
Swagger — `/swagger` в Development. Dev-строка подключения (`FinCtrl.Website/appsettings.Development.json`): локальный SQL Server `Ivan`, база `FinCtrl_db_dev2`, Windows-аутентификация.

Frontend (из `FinCtrl.Frontend/FinCtrl.Frontend/`):
```bash
npm run dev     # Vite dev-сервер на :5173
npm run build
npm run test    # Vitest (jsdom), тесты *.spec.js рядом с кодом
```
Адрес API захардкожен в `config.js` (`APP_CONFIG.apiBaseUrl = http://localhost:5000`); CORS на backend разрешает origins из `appsettings.json` → `Cors:AllowedOrigins` (localhost:3000/5173/5174/8080).

В `.claude/launch.json` описаны preview-конфигурации `backend` и `frontend` для команд выше.

`.claude/settings.json` подключает хук `.claude/hooks/pre-pr-check.mjs`: на `git push` / `gh pr create` он выполняет автоматическую часть «Проверки перед PR» (сборка/тесты затронутых частей, гигиена диффа, папка задачи) и блокирует команду при провале — см. регламент, раздел 4.

Backend-тесты — xUnit + NSubstitute в `FinCtrl/tests/FinCtrl.<Проект>.Tests/` (`FinCtrl.Domain.Tests`, `FinCtrl.Application.Tests`, `FinCtrl.Infrastructure.Tests`, `FinCtrl.Website.Tests`); текущее время в Domain/Application — только через `TimeProvider` (в тестах `FakeTimeProvider`). Frontend-тесты — Vitest + `@vue/test-utils` + `jsdom`, файлы `*.spec.js` рядом с тестируемым кодом. Линтера пока нет ни в backend, ни во frontend.

### Миграции БД
`FinCtrl/db/migrations/000N_описание.sql` — просто SQL-файлы, без раннера и без таблицы учёта применённых миграций. Не править существующие файлы — добавлять новый со следующим номером; накатывать вручную:
```bash
sqlcmd -S Ivan -d FinCtrl_db_dev2 -E -C -i db/migrations/000N_....sql
```

## Архитектура backend

Проекты `FinCtrl.Backend.sln`:

- `FinCtrl.Website` — единственное исполняемое приложение (ASP.NET Core, `Startup.cs`). REST-контроллеры — `*ApiController` в `Controllers/<Name>Api/`, тонкие (логика — в сервисах Application); DTO рядом с контроллером в той же папке, маппинг — `FromModel`/`ToModel` в DTO; PUT/DELETE отвечают 204. В Website только REST API (`AddControllers` + `MapControllers`, атрибутная маршрутизация): MVC/Razor-фронтенда, `Views/` и `wwwroot/` нет (удалены задачей 0011, UI — Vue SPA), `GET /` → 404.
- `src/main/FinCtrl.Domain` — сущности (POCO-классы), read-модели отчётов (`Statistic/`, `Dashboard/`), служебные id категорий — `CategoryIds`.
- `src/main/FinCtrl.Application` — сервисы (бизнес-логика) и интерфейсы репозиториев `Application/<Фича>/I<Entity>Repository.cs`, регистрация в `ServiceCollectionExtensions.AddApplication()`. На Infrastructure не ссылается.
- `src/main/FinCtrl.Infrastructure` — реализации репозиториев на raw SQL через Dapper (Dommel/FluentMap и классов-маппингов нет) и парсинг файлов импорта (`PaymentsImport/`, EPPlus); ссылается на Application, регистрация в `AddInfrastructure()`. Website ссылается на Infrastructure только ради `AddInfrastructure()`.
- `src/common/FinCtrl.Common[.Infrastructure]` — `RepositoryBase` (async-хелперы `Execute`/`ExecuteScalar`/`Query`/`FirstOrDefault` над Dapper), `IDbConnectionFactory`, `DateRange`; `src/common/FinCtrl.Common.Application` — исключения приложения (`NotFoundException`/`ValidationException`/`ConflictException` → 404/400/409 ProblemDetails через `Website/ErrorHandling/ApiExceptionHandler`).

Других backend-проектов в репозитории нет (заброшенная попытка переписать бэкенд на EF Core удалена задачей 0010); тесты — в `tests/` (см. «Команды»).

Новая фича обычно проходит цепочку: SQL-миграция → сущность Domain → интерфейс репозитория в Application → реализация в Infrastructure (+ DI) → сервис Application (+ DI) → `*ApiController` → Vue-страница + роут в `index.js` + пункт меню в `src/App.vue`. Образцы полного CRUD — `Accounts` и `Tags`.

### Доменные правила и грабли
- Итоговая категория: собственная категория платежа приоритетнее категории его источника (`PaymentCategory ?? PaymentSource?.PaymentSourceCategory`).
- `PaymentSource` — метка контрагента из банковского файла (создаётся автоматически при импорте, только чтение/правка); `Account` — реальный банковский счёт (полный CRUD). Импорт для обоих использует паттерн get-or-create по имени — повторять его для будущих lookup-сущностей, создаваемых при импорте.
- Импорт (`POST api/PaymentsApi/upload` → `PaymentsImportService`, `IFileFormatFactory` → `FileFormatFactory` в `Infrastructure/PaymentsImport/` выбирает `XlsxFileFormat`/`CsvFileFormat`) ищет дубли по сумме + дате и у найденных платежей дозаполняет пустой `Account` вместо создания дубля. Маппинг колонок Excel — в `XlsxFileFormat.cs`.
- В `PaymentRepository` **один SELECT платежей** — `SelectPayment` (для всех выборок); его алиасы должны совпадать со свойствами `PaymentDto` (Infrastructure) — это проверяет `PaymentRepositoryTests`. Новое поле Payment нужно добавлять в `SelectPayment`, а также в `PaymentDto` Infrastructure (+ `MapToPayment`) и Website (+ `PaymentDto.FromModel` — единственный маппинг платежа наружу). Теги подгружаются отдельно через `GetTagsForPayments`/`AttachTags`.
- Правка платежа (`PUT api/PaymentsApi/{id}` → `EditPaymentRequest` → `PaymentService.Edit`, одна транзакция): id — из роута; поле `null` = не трогать. `CategoryId`: совпадающая с категорией источника не пишется, очистить нельзя (legacy, не менять); `AccountId`: `0` = очистить; `Description`: пустая строка = очистить; `ExcludeFromStats`: `bool`; `TagIds`: список = заменить целиком. Для новых полей — та же конвенция (null = не трогать).
- `DateRange.To` — всегда исключающая граница; даты `from`/`to` в HTTP-запросах — дни включительно (`DateRange.FromInclusiveDates`).
- У `PaymentTags` нет каскадного удаления в БД — `TagRepository.Delete` сначала удаляет связи.
- Все проекты — `net9.0` с `Nullable`/`ImplicitUsings`; доступ к БД — `Microsoft.Data.SqlClient` (нужен `TrustServerCertificate=True` для локального сервера). В Website отключён неявный `[Required]` для non-nullable полей (`SuppressImplicitRequiredAttributeForNonNullableReferenceTypes`) — обязательные поля входных DTO помечать `[Required]` явно.

## Архитектура frontend
- Vue 3 Composition API (`<script setup>`), Bootstrap 5, axios с прямыми вызовами `APP_CONFIG.apiBaseUrl + '/api/...'` (отдельного API-слоя нет), Chart.js через vue-chartjs, `vue-multiselect` для тегов.
- Роуты описаны в корневом `index.js` (вне `src/`); страницы — в `src/pages/<фича>/`.
- `src/pages/paymentsTable/PaymentsTable.vue` — главная страница (тепловая карта категория × период, модалка правки платежа); `src/pages/payments/Payments.vue` — «Операции» (пагинация/сортировка/фильтры, инлайн и массовая правка, загрузка файла).
