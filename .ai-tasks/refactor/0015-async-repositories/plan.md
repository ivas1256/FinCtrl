# План: [0015] Async-репозитории, единые SELECT'ы, OUTPUT INSERTED

> Статус: `на ревью`. План сверен с кодом после задач 0005–0014 (интерфейсы репозиториев — в Application, реализации — `internal` в Infrastructure).

## Состояние на старте (сверка с кодом)
- Синхронные вызовы Dapper: `RepositoryBase.Execute` (`conn.Execute`) и `FirstOrDefault` (`QueryFirstOrDefault`), `ExecuteScalar` в `Create` у `AccountRepository`, `TagRepository`, `PaymentSourceRepository`. Все Update/Delete идут через синхронный `Execute`.
- `PaymentSourceRepository` уже с одним SELECT'ом и типизированным `PaymentSourceRow` (задача 0009); `.Result` в `PaymentSourceService` уже нет.
- `CategoryRepository`: 4 копии списка колонок (`GetAll`, `GetCategoriesTree`, `GetChildCategories`, `GetById`), маппинг через `dynamic`.
- `AccountRepository`, `TagRepository`: маппинг через `dynamic`; id после вставки — `OUTPUT ... INTO @generated_keys` + синхронный `ExecuteScalar`.
- `PaymentRepository`: `_selectPaymentInfo` и inline `dataSql` в `GetListPaged`; в первом алиас `PaymentSourceCategory_ParentCategoryCategoryId` (свойство DTO — `PaymentSourceCategory_ParentCategoryId`); id после вставки — отдельный `SELECT SCOPE_IDENTITY()`; теги — `dynamic`.
- Триггеров в dev-базе нет — `OUTPUT INSERTED` без `INTO` допустим.

## Затрагиваемые части
- БД: миграций нет.
- Backend / Common.Infrastructure: `RepositoryBase.cs`.
- Backend / Infrastructure: `Accounts/AccountRepository.cs`, `Tags/TagRepository.cs`, `Categories/CategoryRepository.cs`, `PaymentSources/PaymentSourceRepository.cs`, `Payments/PaymentRepository.cs`, `Payments/PaymentTagRow.cs` (новый).
- Тесты: `tests/FinCtrl.Infrastructure.Tests/Payments/PaymentRepositoryTests.cs` (новый).
- Интерфейсы репозиториев (Application) не меняются — моки в тестах сервисов уже async (`Task<...>`), правки не нужны.
- Frontend не затрагивается.

## Шаги
- [x] RepositoryBase: только async (`ExecuteAsync`, `QueryAsync`, `QueryFirstOrDefaultAsync`, + `ExecuteScalar<T>` через `ExecuteScalarAsync`)
- [x] Account/Tag: `QueryAsync<T>` вместо `dynamic`, `OUTPUT INSERTED` + `ExecuteScalarAsync<int>`
- [x] PaymentSource: `OUTPUT INSERTED` + `ExecuteScalarAsync<int>`
- [x] Category: один `const` SELECT, `QueryAsync<Category>` вместо `dynamic`
- [x] Payment: один SELECT для `Get/GetByDateRange/GetBySource/FindDuplicate/GetListPaged`, исправлен алиас, `OUTPUT INSERTED` вместо `SCOPE_IDENTITY()`, теги — `QueryAsync<PaymentTagRow>` пачками ≤ 2000 id
- [x] Юнит-тест: все колонки SELECT'а платежей совпадают со свойствами `PaymentDto`
- [x] Ручная проверка на dev-базе
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [x] Документация: `CLAUDE.md` (грабли про два SELECT'а), `project-summary.md`, `backend-guide.md` (техдолг)
- [x] PR — https://github.com/ivas1256/FinCtrl/pull/18

## Тесты
| Что тестируем | Сценарии |
|---------------|----------|
| `PaymentRepository.SelectPayment` ↔ `PaymentDto` | каждый алиас колонки SELECT'а есть среди свойств `PaymentDto` и наоборот (ловит баг с `ParentCategoryCategoryId`) |
| Существующие тесты сервисов | интерфейсы не менялись — должны остаться зелёными |

SQL репозиториев юнит-тестами не покрывается (регламент §3) — проверяется вручную.

## Ручная проверка
- Smoke всех страниц в preview `backend` + `frontend`, консоль браузера и лог backend без ошибок.
- Счета: создать (id возвращается), переименовать, удалить.
- Теги: создать, переименовать, назначить платежу, удалить.
- Категории: создать, правка, удалить; дерево на странице категорий.
- Платёж: правка категории/счёта/тегов на «Операциях» и на главной.
- Источник: правка категории/описания.
- `GET api/PaymentsApi/...` для операций: у платежей с категорией источника, у которой есть родитель, заполнен `ParentCategoryId`.
- Главная/статистика за весь период (> 2100 платежей) — без 500 на тегах.
- Импорт через `POST api/PaymentsApi/upload` небольшим файлом: создание новых платежей (id из `OUTPUT INSERTED`), дубли не создаются.
- Все тестовые изменения данных вернуть.

## Риски и открытые вопросы
- Большой дифф в SQL. Проверять каждую страницу вручную.
- `QueryAsync<Category>`: Dapper записывает `NULL` из `Color` поверх инициализатора — нормализуем в репозитории.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: ветка создана от `refactor/0014-layer-dependencies`, а не от `main` (отступление от п.1.2 регламента): пользователь выбрал «стек веток», PR #9→…→#17 ещё не смёржены. PR — в `refactor/0014-layer-dependencies`, мёржить после #17.
- 2026-10-20: интерфейсы репозиториев не меняются (сигнатуры уже `Task<...>`), поэтому моки в тестах сервисов не правятся.
- 2026-10-20: `WITH (NOLOCK)` в едином SELECT'е платежей оставлен как был (он был в обоих SELECT'ах) — его убирает задача 0016.
- 2026-10-20: N+1 в `GetCategoriesTree` (рекурсивные запросы) оставлен — задача 0016; здесь только единый SELECT.
- 2026-10-20: заодно исправлен лимит 2100 параметров в `GetTagsForPayments` (из «Вне объёма» задачи 0005): метод всё равно переписывается на типизированный маппинг, а фикс — разбиение id на пачки — тривиален. В dev-базе ~6650 платежей, так что статистика за весь период падала с 500.
- 2026-10-20: `SelectPayment` объявлен `internal const`, а не `private` (отступление от §5 гайда): к нему обращается тест через `InternalsVisibleTo`; читать константу рефлексией было бы хрупче.
- 2026-10-20: в SELECT платежей не добавлен `Color` категорий — у `Payment.PaymentCategory`/категории источника цвет никто не читает (цвета берутся из дерева категорий), расширять выборку без потребителя незачем.
- 2026-10-20: юнит-тест на соответствие SELECT'а и `PaymentDto` — в `FinCtrl.Infrastructure.Tests` (без БД, разбор алиасов из константы) — фиксирует исправление алиаса и защищает от расхождения в будущем.

## Вне объёма (найдено попутно)
- `GET api/PaymentsApi?periodType=0` (группировка не выбрана) → `NotImplementedException` и 500 — относится к 0017 (ошибки/валидация, `NotImplementedException` для группировки).
- В `GET api/PaymentsApi/list` у `paymentSource.createdAt` всегда `0001-01-01`: `PaymentsApiController` маппит `PaymentSourceDto` вручную и не переносит `CreatedAt` (в `PaymentSourceDto.FromModel` поле есть). Относится к 0018 (один маппинг на пару типов).

## Итог
Сделано:
- `RepositoryBase` и все репозитории — только async Dapper; хелпер `ExecuteScalar<T>` в базе.
- Id после вставки — `OUTPUT INSERTED` + `ExecuteScalarAsync<int>` в Account/Tag/PaymentSource/Payment (Category уже так делал).
- Один `const`-SELECT на репозиторий; у платежей — единый `SelectPayment`, исправлен алиас `PaymentSourceCategory_ParentCategoryId`.
- Типизированный маппинг вместо `dynamic` (`QueryAsync<Account|Tag|Category>`, `PaymentTagRow`).
- Теги платежей грузятся пачками по 2000 id (фикс лимита 2100 параметров из 0005).
- Тест `PaymentRepositoryTests` (алиасы SELECT'а ↔ `PaymentDto`; без фикса алиаса падает). Тесты: Domain 8, Application 16, Infrastructure 11 — зелёные.
- Ручная проверка (preview backend + frontend): все 7 страниц открываются, в консоли браузера и логах backend ошибок нет; через API — CRUD счёта/тега/категории (id из `OUTPUT INSERTED`, удаление тега вместе со связью), правка платежа (категория, счёт, теги, описание), фильтр «Операций» по тегу, правка источника, `parentCategoryId` у категории источника в `by-source`, главная за весь период (6650 платежей) — 200, импорт CSV (новый источник и платёж создаются, повторная загрузка дублей не создаёт). Тестовые данные удалены, исходные значения платежа 6536 и источника 925 восстановлены, identity-сиды таблиц возвращены.

Отложено: транзакции, N+1 дерева категорий, `NOLOCK` — задача 0016.
