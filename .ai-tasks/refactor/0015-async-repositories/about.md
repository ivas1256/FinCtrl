# [0015] Async-репозитории, единые SELECT'ы, OUTPUT INSERTED

- **Ветка:** `refactor/0015-async-repositories`
- **Репозитории:** backend
- **Создана:** 2026-10-20
- **Источник:** `.ai-docs/project/architecture-audit.md`, §2.4

## Контекст
Гайд (§5) требует только async Dapper, `OUTPUT INSERTED` + `ExecuteScalarAsync` для получения id и один `const`-SELECT на сущность. В коде:
- синхронные вызовы в `RepositoryBase` (все Update/Delete), в `PaymentSourceRepository`, `AccountRepository` и `TagRepository`;
- 4 копии SELECT'ов в `CategoryRepository` и 4 в `PaymentSourceRepository`;
- два разошедшихся SELECT'а платежей: алиас `PaymentSourceCategory_ParentCategoryCategoryId` в `_selectPaymentInfo` не совпадает с DTO, поле всегда null;
- маппинг через `dynamic`.

## Постановка
- `RepositoryBase` и все репозитории — только async-методы Dapper.
- Получение id: `OUTPUT INSERTED.<Id>` + `ExecuteScalarAsync<int>` (Account, Tag, PaymentSource, Payment).
- Один `private const string` SELECT на сущность; один SELECT платежей для `Get/GetByDateRange/GetBySource/FindDuplicate/GetListPaged` (исправит алиас).
- `QueryAsync<T>` вместо `dynamic`-маппинга.

## Вне объёма
- Транзакции и N+1 (0016).

## Критерии приемки
- [ ] В Infrastructure/Common.Infrastructure нет синхронных вызовов Dapper
- [ ] Каждый репозиторий имеет единый SELECT; у платежей — один источник колонок
- [ ] `PaymentSourceCategory_ParentCategoryId` заполняется
- [ ] Ручная проверка на dev-базе: CRUD счетов, тегов, категорий, правка платежей и источников, импорт
- [ ] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
- [ ] Все тесты зелёные
