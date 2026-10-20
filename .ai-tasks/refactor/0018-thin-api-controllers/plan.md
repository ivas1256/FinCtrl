# План: [0018] Тонкие API-контроллеры и DTO-контракт

> Статус: `в работе`. План сверен с кодом после задач 0005–0017 (интерфейсы репозиториев в Application, async, `IUnitOfWork`, исключения + `ApiExceptionHandler`).

## Затрагиваемые части
Backend:
- `Domain/Payment.cs` (`record` → `class`), `Category.cs` + новый `CategoryIds.cs`, `Statistic/*` (read-модель таблицы, удалить `MainStatisticPayment`), `Dashboard/DashboardSummary.cs`.
- `Common/DateRange.cs` — фабрика из включительных дат.
- `Application/Statistic/*` — `GetPaymentsTable` (дерево, ключи периодов, фейковые категории, период по умолчанию).
- `Application/Dashboard/*` — проценты, топ-платежи, дефолтные периоды графика, исключающая `To`.
- `Application/Payments/*` — `Edit(PaymentEdit)` под `IUnitOfWork`; `GetListPaged` с открытыми границами дат.
- `Infrastructure/Payments/PaymentRepository.cs` — условия по датам только при заданных границах.
- `Website/Controllers/*Api/*` — тонкие контроллеры, DTO по папкам фич, `*Response`, `NoContent()`; удалить `RequestDto/`.
- `tests/FinCtrl.Website.Tests/` — новый проект (+ `.sln`).

Frontend:
- `pages/payments/Payments.vue` — `excludeFromStats: bool` вместо `excludeFromStatsStr`.
- `pages/paymentsTable/PaymentsTable.vue`, `PaymentList.vue` — 204 вместо проверки `status === 200`, `effectiveCategory`.
- `pages/paymentSources/PaymentSources.vue` — модалка платежей источника по `PaymentDto`.

## Шаги
- [ ] Domain: `CategoryIds`, `Payment` → `class`, read-модель таблицы
- [ ] Statistic: `GetPaymentsTable` в сервисе + тесты
- [ ] Dashboard: проценты, топ, периоды + тесты
- [ ] `PaymentService.Edit` + тесты; `GetListPaged` без 1753/9999
- [ ] Website: DTO по папкам, один маппинг, `*Response`, `NoContent()`, тонкие контроллеры
- [ ] `FinCtrl.Website.Tests`: маппинг
- [ ] Frontend: bool-контракт, 204, `effectiveCategory`
- [ ] Ручная проверка, документация
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
- `MainStatisticServiceTests`: `GetPaymentsTable` — сумма узла = своя + потомки, платежи потомков в ячейке, все периоды в каждом узле, фейковые «Без категории»/«ВСЕГО» в конце, ключи периодов (месяц/неделя/квартал/год/день), период по умолчанию от `TimeProvider`, `Day` — период `[d, d+1)`.
- `DashboardServiceTests`: проценты (и при нулевых тратах), топ-платежи по модулю, дефолтные периоды графика по режимам, исключающая `To` в графике.
- `PaymentServiceTests`: `Edit` — null не трогает поля (описание, флаг, категория, счёт, теги), `AccountId = 0` очищает, категория источника не записывается в платёж, пустое описание очищает, несуществующие тег/категория/счёт → 400 без записи, правка + теги в одной транзакции (commit), ошибка тегов — без commit, NotFound.
- `FinCtrl.Website.Tests`: `PaymentDto.FromModel` (категория своя/источника, `categorySource`, `createdAt` источника, теги), `CategoryDto.FromModel` (дерево), маппинг таблицы главной.
- Frontend: логики для юнит-тестов не добавляется (правки — поля запросов).

## Ручная проверка
- preview `backend` + `frontend`: все страницы, инлайн и массовая правка платежей, модалка правки на главной, CRUD счетов/тегов/категорий, импорт через API, модалка платежей источника.
- curl: PUT платежа без `description`/`excludeFromStats` — поля не меняются; PUT с несуществующим тегом — 400 и платёж не изменён; PUT/DELETE — 204; `GET list` — `paymentSource.createdAt` заполнен.
- Тестовые изменения данных вернуть.

## Риски и открытые вопросы
- Крупная задача (постановка предлагала 2–3 PR). Решение — см. журнал.
- Смена формы ответов (`by-source`, `CategoriesApi`) может задеть страницы: пройти все.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: ветка создана от `refactor/0017-errors-validation`, а не от `main` (отступление от п.1.2 регламента): пользователь выбрал «стек веток», PR #9→…→#20 ещё не смёржены. PR — в `refactor/0017-errors-validation`, мёржить после #20.
- 2026-10-20: одна задача — один PR (не делим на 2–3): стек уже из 12 PR, пользователь поручил задачу целиком; части разнесены по логическим коммитам.
- 2026-10-20: в постановку включены пункты, отложенные задачами 0015–0017: атомарный `EditPayment` в `PaymentService.Edit`, затирание описания, 204, `createdAt` источника в списке, `CategoryDto` из `RequestDto/`, проект `FinCtrl.Website.Tests`, §10 гайда.
