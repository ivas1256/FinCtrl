# План: [0009] Потеря исключений в PaymentSourceService.MapCategories

> Статус: `на ревью`.

## Сверка с текущим кодом (2026-10-20)
- Описанный в аудите вызов `MapCategories` без `await` и `.Result` в текущем коде уже исправлены: `MapCategories` вызывается через `await`, внутри — `await _categoryService.GetById(...)`. Остались: N+1 (запрос категории на каждый источник) и тихая подмена — если категория не найдена, источник отдаётся с «пустой» категорией, у которой заполнен только id.
- `ICategoryService` в `PaymentSourceService` используется ещё в `Edit` (добавлен задачей 0006) — только чтобы собрать объект `Category` для `Update`, которому нужен лишь `CategoryId`.
- Методы репозитория `GetAll`/`GetById`/`GetByName` синхронно вызывают `conn.Query` (без JOIN), SELECT копипастится в 4 местах.

## Затрагиваемые части
- `FinCtrl/src/main/FinCtrl.Application/PaymentSources/PaymentSourceService.cs` — удалить `MapCategories` и зависимость `ICategoryService`.
- `FinCtrl/src/main/FinCtrl.Infrastructure/PaymentSources/PaymentSourceRepository.cs` — общий SELECT с `LEFT JOIN Categories`, async Dapper в затронутых методах.
- `FinCtrl/src/main/FinCtrl.Infrastructure/PaymentSources/PaymentSourceRow.cs` (новый) — плоская строка с алиасами `Category_*` + `MapToPaymentSource()`.
- `FinCtrl/tests/FinCtrl.Application.Tests/PaymentSources/PaymentSourceServiceTests.cs`.

## Шаги
- [x] Красный тест: чтение источников не догружает категории через `ICategoryService` (N+1), категория из репозитория возвращается как есть
- [x] JOIN в репозитории (общий SELECT, `PaymentSourceRow`, async)
- [x] Удалить `MapCategories` и `ICategoryService` из `PaymentSourceService`; `Edit` собирает `Category` по id
- [x] Ручная проверка SQL на dev-базе и страницы «Источники»
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [x] PR

## Тесты
- `PaymentSourceServiceTests`: `GetAll`/`GetById`/`GetByName` возвращают категорию, загруженную репозиторием, без дополнительных запросов (красный — до фикса сервис зовёт `ICategoryService.GetById`).
- `Edit`: с категорией — в `Update` уходит категория с переданным id; без категории — категория снимается.
- SQL репозитория юнит-тестами не покрывается — проверка вручную на dev-базе (сравнение категорий источников до/после, `GET api/PaymentSourcesApi`, `/list`, страница «Источники»).

## Ручная проверка
Сценарии из критериев приемки — в preview `backend` + `frontend` / на dev-базе. Выполнено 2026-10-20:
- SQL с `LEFT JOIN Categories` выполнен на `FinCtrl_db_dev2`: 1276 источников, 882 с категорией, «осиротевших» ссылок на категории нет.
- `GET api/PaymentSourcesApi` — 1276 источников, 882 с `categoryId` (совпадает с БД); `GET list` с пагинацией, `noCategoryOnly=true` (394 = 1276 − 882) и `search` — корректно.
- `PUT api/PaymentSourcesApi/1384`: установка категории 18 → 204, категория видна в `list`; несуществующая категория → 500 (FK, см. «Риски»); снятие категории → 204. Данные источника 1384 (имя, категория, `LastUpdatedAt`) возвращены к исходным.
- Страница «Источники» (`/payment-sources`) показывает категории источников; ошибок в консоли браузера и логах backend нет.

## Риски и открытые вопросы
- `Edit` с несуществующим `categoryId`: раньше категория молча снималась (`GetById` → null), теперь UPDATE упадёт на FK `FK_PaymentSources_Categories_CategoryId` (500). Тихая потеря данных хуже явной ошибки; нормальный 400/404 — задача 0017.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: по выбору пользователя («стек веток») ветка ответвлена не от `main`, а от `fix/0008-source-payments-deleted` (PR #11, не смёржен); PR — в эту ветку, мёржить после #11. Отступление от п. 1.2 регламента.
- 2026-10-20: красный тест сформулирован как «сервис не обращается к `ICategoryService` при чтении источников» — `.Result`/потеря `await` уже исправлены, воспроизводимая проблема — N+1 и подмена категории. После удаления зависимости тест переписан под новый конструктор.
- 2026-10-20: маппинг JOIN'а — через плоский internal-класс `PaymentSourceRow` с алиасами `Category_*` (backend-guide §5), без `dynamic`; затронутые методы репозитория (`GetAll`, `GetById`, `GetByName`, `GetListPaged`) переведены на `QueryAsync` (разрешено постановкой). `Create` не трогаем (0015).
- 2026-10-20: `Edit` больше не загружает категорию: репозиторию для UPDATE нужен только `CategoryId`, поэтому категория собирается как `new Category(categoryId)`. Это и убирает последнюю причину зависимости от `ICategoryService`.

## Вне объёма (найдено попутно)
- `PaymentSourceRepository.Create` по-прежнему синхронный (`conn.ExecuteScalar`) — задача 0015.
- Правка источника с несуществующим `categoryId` отдаёт 500 (FK) вместо 400/404 — задача 0017.
- Legacy MVC `PaymentSourceController` сам грузит категории через `ICategoryService` — удаляется задачей 0011.

## Итог
- `MapCategories` и зависимость `ICategoryService` удалены из `PaymentSourceService`; категория источника загружается в репозитории одним запросом (`LEFT JOIN Categories`, общий `SelectPaymentSource`, `PaymentSourceRow`), методы чтения — на `QueryAsync`.
- Тесты: 4 новых теста чтения в `PaymentSourceServiceTests` (3 были красными до фикса), тесты `Edit` обновлены. `dotnet build` — 0 ошибок, нет предупреждений в изменённых файлах; `dotnet test` — 19/19 зелёные.
- Frontend не менялся. Осталось: см. «Вне объёма».
