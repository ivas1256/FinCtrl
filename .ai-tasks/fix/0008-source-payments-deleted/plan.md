# План: [0008] Удалённые платежи в списке платежей источника

> Статус: `в работе`. План сверен с кодом 2026-10-20: `PaymentRepository.GetBySource` — по-прежнему `:275-283`, фильтра `IsDeleted` нет.

## Затрагиваемые части
- `FinCtrl/src/main/FinCtrl.Infrastructure/Payments/PaymentRepository.cs` — `GetBySource`.
- Вызывающие (без изменений): `PaymentService.GetBySource` → `GET api/PaymentsApi/source/{id}` (`PaymentsApiController.GetBySource`) → модалка платежей источника в `PaymentSources.vue`.

## Шаги
- [ ] Найти на dev-базе источник с удалённым платежом
- [ ] Фикс SQL: `WHERE p.PaymentSourceId = @PaymentSourceId AND p.IsDeleted = 0`
- [ ] Ручная проверка в preview (API + модалка)
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
Изменение только в SQL репозитория — по регламенту (§3) юнит-тестами не покрывается: тестов с реальной БД в проекте нет, а в `PaymentService.GetBySource` логики нет (прямой проброс в репозиторий), мок-тест сервиса фикс бы не проверял. Вместо «красного» теста — воспроизведение бага на dev-базе до фикса (см. «Ручная проверка»).

## Ручная проверка
1. Найти источник с удалёнными платежами:
   ```sql
   SELECT p.PaymentSourceId,
          SUM(CASE WHEN p.IsDeleted = 1 THEN 1 ELSE 0 END) AS Deleted,
          SUM(CASE WHEN p.IsDeleted = 0 THEN 1 ELSE 0 END) AS Alive
   FROM Payments p
   GROUP BY p.PaymentSourceId
   HAVING SUM(CASE WHEN p.IsDeleted = 1 THEN 1 ELSE 0 END) > 0;
   ```
2. До фикса: `GET /api/PaymentsApi/source/{id}` возвращает `Deleted + Alive` записей (баг воспроизведён).
3. После фикса: тот же запрос возвращает ровно `Alive` записей, удалённого `PaymentId` среди них нет.
4. В UI: «Источники платежа» → открыть платежи этого источника — удалённого платежа нет, ошибок в консоли нет.

Данные dev-базы не меняются (только чтение).

## Риски и открытые вопросы
- `GetBySource` вызывается только из `GET api/PaymentsApi/source/{id}`, других потребителей, которым нужны удалённые платежи, нет.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: отступление от п. 1.2 регламента по выбору пользователя («стек веток»): ветка создана не от `main`, а от `fix/0006-payment-source-single-save` (PR #10, стекается на #9, не смёржены); PR открывается в `fix/0006-payment-source-single-save` и мёржится после #10. Дифф `main...HEAD`, который смотрит хук, включает изменения 0005/0006 — это ожидаемо.
- 2026-10-20: юнит-тест не пишется — фикс только в SQL (см. «Тесты»); баг воспроизводится на dev-базе до фикса.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
