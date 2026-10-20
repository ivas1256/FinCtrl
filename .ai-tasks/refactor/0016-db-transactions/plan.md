# План: [0016] Транзакции, N+1 и доменные правила вне Infrastructure

> Статус: `в работе`. План сверен с кодом после задач 0005–0015 (интерфейсы репозиториев в Application, все репозитории async, единый `PaymentRepository.SelectPayment`).

## Затрагиваемые части (backend)
- `Common/` — `IUnitOfWork`, `ITransaction` (абстракция транзакции, без зависимостей от БД).
- `Common.Infrastructure/IDbConnectionFactory.cs`, `RepositoryBase.cs` — текущая транзакция передаётся во все вызовы Dapper; хелпер `BeginTransaction()`.
- `Infrastructure/DbConnectionFactory.cs` (реализация транзакции, вложенность), `ServiceCollectionExtensions.cs` (DI `IUnitOfWork`).
- `Infrastructure/Payments/PaymentRepository.cs` (`SetPaymentTags`, `AddTagsToPayments` в транзакции; убрать `NOLOCK`), `PaymentDto.cs` (знак суммы — вызов доменного метода).
- `Infrastructure/Tags/TagRepository.cs` (`Delete` в транзакции).
- `Infrastructure/PaymentSources/PaymentSourceRepository.cs` (прямые `conn.QueryAsync` → хелперы базы, иначе они не видят транзакцию).
- `Infrastructure/Categories/CategoryRepository.cs`, `Application/Categories/*` — дерево одним запросом, сборка в `CategoryService`.
- `Application/PaymentsImport/PaymentsImportService.cs` (+ интерфейс) — транзакция на весь файл, `ILogger`, `CancellationToken`, кэш источников/счетов.
- `Domain/Payment.cs` — `NormalizeSumSign()`.
- `Website/Controllers/PaymentsApi/PaymentsApiController.cs` — передать `CancellationToken` в импорт.

## Шаги
- [ ] Транзакционная абстракция (`IUnitOfWork`/`ITransaction` в `FinCtrl.Common`, реализация в `DbConnectionFactory`, `RepositoryBase` передаёт транзакцию)
- [ ] Транзакции в репозиториях (`SetPaymentTags`, `AddTagsToPayments`, `TagRepository.Delete`) и импорте (весь файл)
- [ ] Дерево категорий: один SELECT + сборка в памяти (`CategoryService`)
- [ ] Знак суммы в Domain (`Payment.NormalizeSumSign`)
- [ ] NOLOCK убран
- [ ] Логи и CancellationToken в импорте; меньше запросов на строку
- [ ] Тесты и ручная проверка
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
- Domain: `PaymentTests.NormalizeSumSign_*` — расход с положительной суммой → отрицательная; расход с отрицательной и доход — без изменений.
- Application: `CategoryServiceTests.GetCategoriesTree_*` — сборка дерева из плоского списка (вложенность, порядок по `OrderIndex`, сироты не попадают); репозиторий вызывается один раз.
- Application: `PaymentsImportServiceTests` — транзакция открывается и коммитится; ошибка на N-й строке → `Commit` не вызывается, транзакция освобождается (откат); отмена через `CancellationToken` → без коммита; источник/счёт запрашиваются один раз на файл (кэш); итог в лог (`FakeLogger`).

## Ручная проверка
- preview `backend` + `frontend`: смоук всех страниц, нет ошибок в консоли/логах.
- API: CRUD тега (удаление со связями), правка платежа с тегами, массовое назначение тегов, дерево категорий (время ответа), импорт CSV через API.
- Атомарность на dev-базе: CSV, где последняя строка вызывает ошибку SQL (дата вне диапазона `datetime`) → 500, количество платежей/источников/счетов не изменилось; итог импорта в логе backend.
- Тестовые данные удалить, identity-сиды вернуть.

## Риски и открытые вопросы
- Дизайн unit-of-work затрагивает все репозитории. Решено без согласования (пользователь просил не задавать вопросов), см. журнал решений.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: ветка создана от `refactor/0015-async-repositories`, а не от `main` (отступление от п.1.2 регламента): пользователь выбрал «стек веток», PR #9→…→#18 ещё не смёржены. PR — в `refactor/0015-async-repositories`, мёржить после #18.
- 2026-10-20: транзакции — через `IUnitOfWork.BeginTransaction()` → `ITransaction` (`Commit`, `DisposeAsync` без коммита = откат). Соединение и так одно на scope (`DbConnectionFactory` — scoped), поэтому транзакцию хранит фабрика, а `RepositoryBase` передаёт её во все вызовы Dapper — сигнатуры репозиториев не меняются. Вложенный `BeginTransaction` при открытой транзакции возвращает no-op: репозиторий может сам защищать свою многошаговую запись, а сервис — объединить несколько репозиториев в одну транзакцию.
- 2026-10-20: интерфейсы `IUnitOfWork`/`ITransaction` положены в `FinCtrl.Common` (общий для всех слоёв, без зависимостей), а не в новый проект `FinCtrl.Common.Application`: их используют и Application (сервисы), и `Common.Infrastructure` (`IDbConnectionFactory`), а `Common.Infrastructure` не ссылается на Application-слой.
- 2026-10-20: `_connectionFactory` в `RepositoryBase` стал `private`: прямые `conn.QueryAsync` в `PaymentSourceRepository` не передавали бы транзакцию (SqlClient падает «ExecuteReader requires the command to have a transaction»), поэтому все запросы — только через хелперы базы.
- 2026-10-20: импорт — одна транзакция на весь файл (вместе с get-or-create источников и счетов): частичный импорт хуже долгой транзакции для личного приложения.
- 2026-10-20: `CancellationToken` в импорте проверяется перед каждой строкой и передаётся в `BeginTransaction`/`Commit`; в методы репозиториев не протаскивается (затронуло бы все интерфейсы, отмена между строками достаточна).
- 2026-10-20: меньше запросов на строку импорта: источник и счёт — кэш по имени на время файла (сравнение `Ordinal`, иначе по-разному написанное имя просто идёт в БД, как раньше) и `GetByName` вместо `Exists` + `GetByName`. На строку остаются `FindDuplicate` + `Create`/`Update` (было до 6 запросов).
- 2026-10-20: знак суммы — `Payment.NormalizeSumSign()`, вызывается при чтении из БД (в маппере), как и раньше. При импорте нормализация не применяется: в dev-базе 1455 расходов с положительной суммой, а поиск дублей сравнивает сумму как есть — нормализация при записи сломала бы его для старых данных.
- 2026-10-20: дерево категорий собирает `CategoryService` из плоского `GetAll()` (один SELECT, сортировка по `OrderIndex`, затем `CategoryId`), `ICategoryRepository.GetCategoriesTree` удалён. Сироты (родитель не найден) и циклы в дерево не попадают — раньше сирот тоже не было, а цикл давал бесконечную рекурсию.
- 2026-10-20: `NOLOCK` убран и из `SelectPayment`, и из COUNT в `GetListPaged`: грязное чтение не нужно (конкурентных писателей нет), а для поиска дублей вредно.
- 2026-10-20: `EditPayment` (Update + SetPaymentTags) в одну транзакцию не объединён: оркестрация живёт в контроллере, перенос в `PaymentService.Edit` — задача 0018 (там и обернуть в `IUnitOfWork`). `SetPaymentTags` сам по себе атомарен.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
