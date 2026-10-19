# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

FinCtrl — личное приложение для учёта финансов: пользователь загружает выгрузку из банка (Excel/CSV), операции привязываются к источникам платежа / счетам / категориям / тегам, UI показывает сводные таблицы. Корневая папка — единый git-репозиторий (базовая ветка `main`) с двумя частями:

- `FinCtrl/` — backend, .NET 9 + MS SQL Server (Dapper, без ORM).
- `FinCtrl.Frontend/FinCtrl.Frontend/` — frontend, Vue 3 + Vite.

Подробный контекст проекта — в `.ai-docs/`, сгруппирован по двум папкам:

- `.ai-docs/project/` — описание проекта и документация: `project-summary.md` (домен, API, известные грабли, история доработок), `general architecture.md` (исходное описание идеи и домена), `payments-table-heatmap.md` (спецификация тепловой карты на главной). При существенных изменениях актуализировать `project-summary.md`.
- `.ai-docs/workflow/` — регламенты работы: `task-workflow.md` (регламент работы над задачей, см. ниже), `styleguide.md` (UI- и backend-конвенции — следовать при создании новых страниц/компонентов).

## Регламент работы над задачей

**Обязателен для любой задачи, меняющей код** (ветка, папка задачи в `.ai-tasks/`, тесты, сборка, PR, журнал `.ai-tasks/tasks.md`). Полный текст:

@.ai-docs/workflow/task-workflow.md

## Команды

Backend (из `FinCtrl/`):
```bash
dotnet build FinCtrl.Backend.sln
dotnet run --project FinCtrl.Website/FinCtrl.Website.csproj --urls http://localhost:5000
```
Swagger — `/swagger` в Development. Dev-строка подключения (`FinCtrl.Website/appsettings.Development.json`): локальный SQL Server `Ivan`, база `FinCtrl_db_dev2`, Windows-аутентификация.

Frontend (из `FinCtrl.Frontend/FinCtrl.Frontend/`):
```bash
npm run dev     # Vite dev-сервер на :5173
npm run build
```
Адрес API захардкожен в `config.js` (`APP_CONFIG.apiBaseUrl = http://localhost:5000`); CORS на backend разрешает localhost:3000/5173/5174/8080.

В `.claude/launch.json` описаны preview-конфигурации `backend` и `frontend` для команд выше.

Автотестов и линтера пока нет ни в backend, ни во frontend — первая задача, меняющая соответствующую часть, заводит тестовую инфраструктуру (см. регламент, раздел 3).

### Миграции БД
`FinCtrl/db/migrations/000N_описание.sql` — просто SQL-файлы, без раннера и без таблицы учёта применённых миграций. Не править существующие файлы — добавлять новый со следующим номером; накатывать вручную:
```bash
sqlcmd -S Ivan -d FinCtrl_db_dev2 -E -C -i db/migrations/000N_....sql
```

## Архитектура backend

Рабочие только проекты из `FinCtrl.Backend.sln`:

- `FinCtrl.Website` — единственное исполняемое приложение (ASP.NET Core, `Startup.cs`). REST-контроллеры — `*ApiController` в `Controllers/<Name>Api/`; DTO в `RequestDto/` и рядом с контроллерами. MVC-контроллеры и Razor `Views/` — legacy, не используются (их заменило Vue SPA), не расширять.
- `src/main/FinCtrl.Domain` — сущности (POCO/record).
- `src/main/FinCtrl.Application` — сервисы (бизнес-логика), регистрация в `ServiceCollectionExtensions.AddApplication()`.
- `src/main/FinCtrl.Infrastructure` — репозитории на raw SQL через Dapper + `Dapper.FluentMap.Dommel`-маппинги на каждую таблицу; регистрация в `AddInfrastructure()`.
- `src/common/FinCtrl.Common[.Infrastructure]` — `RepositoryBase` (хелперы запросов Dapper + generic Update/Delete через Dommel), `IDbConnectionFactory`, `Pagination`, `DateRange`.

**Мёртвый код — не трогать и не путать с рабочим стеком:** `FinCtrl.Backend.Core.RestAPI`, `FinCtrl.DAL`, `src/Payments/*`, `src/FinCtrl.CoreLib` (заброшенная попытка переписать на EF Core, в .sln не входит).

Новая фича обычно проходит цепочку: SQL-миграция → сущность Domain → репозиторий Infrastructure (+ DI) → сервис Application (+ DI) → `*ApiController` → Vue-страница + роут в `index.js` + пункт меню в `src/App.vue`. Образцы полного CRUD — `Accounts` и `Tags`.

### Доменные правила и грабли
- Итоговая категория: собственная категория платежа приоритетнее категории его источника (`PaymentCategory ?? PaymentSource?.PaymentSourceCategory`).
- `PaymentSource` — метка контрагента из банковского файла (создаётся автоматически при импорте, только чтение/правка); `Account` — реальный банковский счёт (полный CRUD). Импорт для обоих использует паттерн get-or-create по имени — повторять его для будущих lookup-сущностей, создаваемых при импорте.
- Импорт (`POST api/PaymentsApi/upload` → `PaymentsImportService`, `FileFormatFactory` выбирает `XlsxFileFormat`/`CsvFileFormat`) ищет дубли по сумме + дате и у найденных платежей дозаполняет пустой `Account` вместо создания дубля. Маппинг колонок Excel — в `XlsxFileFormat.cs`.
- В `PaymentRepository` **два независимых SELECT'а платежей**: `_selectPaymentInfo` (Get/GetByDateRange/GetBySource/FindDuplicate) и inline `dataSql` в `GetListPaged`. Новое поле Payment нужно добавлять в оба, а также в `PaymentDto` (Infrastructure и Website), `MainStatisticPayment` и в оба пути маппинга в `PaymentsApiController` (`GetList` и `BuildTree`). Теги подгружаются отдельно через `GetTagsForPayments`/`AttachTags`.
- Семантика null в `PaymentEditDto` неоднородна: `CategoryId == null` → не трогать (очистить нельзя; legacy, не менять); `AccountId` → null = не трогать, `0` = очистить; `TagIds` → null = не трогать, список = заменить целиком. Для новых полей использовать конвенцию `AccountId`/`TagIds`.
- `EditPayment` биндит всё тело через `[FromBody]`, поэтому клиент обязан передавать `paymentId` в теле PUT (значение из роута игнорируется).
- У `PaymentTags` нет каскадного удаления в БД — `TagRepository.Delete` сначала удаляет связи.

## Архитектура frontend
- Vue 3 Composition API (`<script setup>`), Bootstrap 5, axios с прямыми вызовами `APP_CONFIG.apiBaseUrl + '/api/...'` (отдельного API-слоя нет), Chart.js через vue-chartjs, `vue-multiselect` для тегов.
- Роуты описаны в корневом `index.js` (вне `src/`); страницы — в `src/pages/<фича>/`.
- `src/pages/paymentsTable/PaymentsTable.vue` — главная страница (тепловая карта категория × период, модалка правки платежа); `src/pages/payments/Payments.vue` — «Операции» (пагинация/сортировка/фильтры, инлайн и массовая правка, загрузка файла).
