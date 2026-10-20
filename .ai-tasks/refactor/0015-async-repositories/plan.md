# План: [0015] Async-репозитории, единые SELECT'ы, OUTPUT INSERTED

> Статус: `в работе`. План сверен с кодом после задач 0005–0014 (интерфейсы репозиториев — в Application, реализации — `internal` в Infrastructure).

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
- [ ] RepositoryBase: только async (`ExecuteAsync`, `QueryAsync`, `QueryFirstOrDefaultAsync`, + `ExecuteScalar<T>` через `ExecuteScalarAsync`)
- [ ] Account/Tag: `QueryAsync<T>` вместо `dynamic`, `OUTPUT INSERTED` + `ExecuteScalarAsync<int>`
- [ ] PaymentSource: `OUTPUT INSERTED` + `ExecuteScalarAsync<int>`
- [ ] Category: один `const` SELECT, `QueryAsync<Category>` вместо `dynamic`
- [ ] Payment: один SELECT для `Get/GetByDateRange/GetBySource/FindDuplicate/GetListPaged`, исправлен алиас, `OUTPUT INSERTED` вместо `SCOPE_IDENTITY()`, теги — `QueryAsync<PaymentTagRow>` пачками ≤ 2000 id
- [ ] Юнит-тест: все колонки SELECT'а платежей совпадают со свойствами `PaymentDto`
- [ ] Ручная проверка на dev-базе
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] Документация: `CLAUDE.md` (грабли про два SELECT'а), `project-summary.md`, `backend-guide.md` (техдолг)
- [ ] PR

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
- 2026-10-20: юнит-тест на соответствие SELECT'а и `PaymentDto` — в `FinCtrl.Infrastructure.Tests` (без БД, разбор алиасов из константы) — фиксирует исправление алиаса и защищает от расхождения в будущем.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
