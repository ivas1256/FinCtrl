# План: [0017] Обработка ошибок и валидация по гайду

> Статус: `на ревью`. План сверен с кодом после задач 0005–0016 (интерфейсы репозиториев в Application, async-репозитории, `IUnitOfWork`, Nullable в Website).

## Затрагиваемые части
Backend:
- `src/common/FinCtrl.Common.Application/` — новый проект: `NotFoundException`, `ValidationException`, `ConflictException` (+ в `.sln`, ссылка из Application).
- `Website/ErrorHandling/ApiExceptionHandler.cs` + `Startup.cs` — `AddProblemDetails`, `IExceptionHandler`, `UseExceptionHandler`/`UseStatusCodePages` вместо `UseDeveloperExceptionPage`.
- `Application/Accounts`, `Tags`, `Categories`, `PaymentSources`, `Payments`, `Statistic`, `PaymentsImport` — валидация, NotFound, проверки связей перед Delete.
- `Application/*/I*Repository.cs` + `Infrastructure/*Repository.cs` — методы проверки связей (`HasPayments`, `GetUsage`), `GetExistingIds` для тегов.
- `Infrastructure/PaymentsImport/*` — `ValidationException` вместо `NotImplementedException`, культура и ошибки разбора CSV/xlsx.
- `Website/Controllers/*Api/*`, `RequestDto/*` — проверки из контроллеров убраны, DataAnnotations на входных DTO.

Frontend:
- `src/utils/apiError.js` (+ spec) — текст ошибки из ProblemDetails.
- Страницы `accounts`, `tags`, `categories`, `payments`, `paymentSources`, `paymentsTable`, `components/TagMultiSelect.vue` — показывать текст ошибки сервера.

## Шаги
- [x] Исключения (`FinCtrl.Common.Application`)
- [x] Глобальный обработчик (ProblemDetails 400/404/409/500)
- [x] Валидация в сервисах (обязательность, уникальность, существование связанных сущностей; NotFound на правке/удалении)
- [x] Проверка связей перед Delete (счёт, категория)
- [x] Импорт: формат файла, ошибки разбора, культура CSV, CSV только с заголовком
- [x] Группировка `NotSelected` → 400
- [x] DataAnnotations
- [x] Frontend
- [x] Тесты и ручная проверка
- [x] Сборка + тесты + ручная проверка (Definition of Done)
- [x] PR

## Тесты
- Application: `AccountServiceTests`, `TagServiceTests` (новые) — обязательность, дубликат → `ConflictException`, правка/удаление несуществующего → `NotFoundException`, удаление счёта с операциями → `ConflictException`, переименование в занятое имя.
- Application: `CategoryServiceTests` — пустое имя, несуществующий/собственный/дочерний родитель, удаление со связями (дочерние, источники, операции), NotFound.
- Application: `PaymentSourceServiceTests` — несуществующий источник → NotFound, несуществующая категория → Validation, пустое имя, некорректная пагинация.
- Application: `PaymentServiceTests` (новый) — `Get` несуществующего → NotFound, теги/категория/счёт не существуют → Validation, пагинация.
- Application: `MainStatisticServiceTests` — `NotSelected`/неизвестная группировка → `ValidationException` (баг: сначала красный).
- Application: `PaymentsImportServiceTests` — пустой файл → Validation.
- Infrastructure: `FileFormatFactoryTests` — неизвестный формат → `ValidationException`; `CsvFileFormatTests` — `-55.5`/`-55,50` и `dd.MM.yyyy` в любой текущей культуре, файл только с заголовком без перевода строки, некорректная сумма/дата и мало колонок → `ValidationException` с номером строки (баги: сначала красные); `XlsxFileFormatTests` — не-xlsx содержимое → `ValidationException`.
- Frontend: `apiError.spec.js` — ProblemDetails (`detail`/`title`), ошибки валидации модели (`errors`), строка, сетевая ошибка → fallback.

## Ручная проверка
- preview `backend` + `frontend`: смоук всех страниц, нет ошибок в консоли/логах.
- API (curl): PUT/DELETE несуществующих id во всех API → 404 ProblemDetails; дубликат счёта/тега → 409; удаление категории/счёта со связями → 409; неизвестный формат и битый CSV → 400; `GET api/PaymentsApi` без `periodType` → 400; источник с несуществующей категорией, платёж с несуществующим тегом → 400; непредвиденная ошибка → 500 ProblemDetails без страницы исключения.
- UI: дубликат тега/счёта, удаление счёта с операциями — сообщение сервера в alert.
- Тестовые данные удалить.

## Риски и открытые вопросы
- Изменение кодов ответов может задеть обработку ошибок на frontend. Пройти все страницы.

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: ветка создана от `refactor/0016-db-transactions`, а не от `main` (отступление от п.1.2 регламента): пользователь выбрал «стек веток», PR #9→…→#19 ещё не смёржены. PR — в `refactor/0016-db-transactions`, мёржить после #19.
- 2026-10-20: исключения — в новом проекте `src/common/FinCtrl.Common.Application` (по §2 гайда: базовые исключения — общий код Application-слоя, без предметной области), а не в `Application/Common/` из постановки: техническая папка верхнего уровня в Application противоречит «кричащей архитектуре». Infrastructure (форматы импорта) и Website (обработчик) получают их транзитивно через ссылку на Application.
- 2026-10-20: три исключения: `ValidationException` (400) — некорректные данные и несуществующие связанные сущности (категория/счёт/тег/родитель в теле запроса — это ошибка ввода, а не «ресурс не найден»), `NotFoundException` (404) — только сущность из роута, `ConflictException` (409) — дубликат имени и удаление используемой сущности.
- 2026-10-20: `GetById` сервисов бросает `NotFoundException` (явный контракт), `GetByName`/`GetByExcelName` оставлены nullable — на них построен get-or-create импорта. `Update`/`Delete` сервисов счетов/тегов/категорий принимают одну сущность/id (контроллер больше не проверяет `null`); `Exists` убран из интерфейсов сервисов — проверка уникальности внутри `Create`/`Update`.
- 2026-10-20: связи перед удалением считаются по всем операциям, включая удалённые (`IsDeleted = 1`): FK на них тоже срабатывает. Сообщение содержит количество связей. Удаление категории с дочерними категориями — тоже 409 (FK на родителя).
- 2026-10-20: существование категории/счёта/тегов проверяется в `PaymentService` (`Update`, `SetPaymentTags`, `BulkAssignTags`) и `PaymentSourceService.Edit` через репозитории других фич, а не через их сервисы — сервису нужен только факт существования, а `GetById` сервиса бросает 404 вместо 400. Теги проверяются по `GetAll()` (их единицы), без нового метода репозитория.
- 2026-10-20: `GET api/PaymentsApi` без `periodType` — 400 «Не выбран тип группировки», а не группировка по умолчанию: так прописано в постановке (замена `NotImplementedException` на `ValidationException`), фронтенд всегда передаёт тип. Проверка — до выборки: иначе ошибка зависела бы от наличия платежей.
- 2026-10-20: файл без операций (пустой или только заголовок) → 400 «В файле нет операций»: раньше такой импорт молча «успешен», и пользователь не замечал, что выбрал не тот файл. Парсер CSV при этом возвращает пустой список (не падает) — проверка в `PaymentsImportService`.
- 2026-10-20: CSV — дата сначала в `ru-RU` (формат банковской выгрузки), затем Invariant; сумма — пробелы убираются, запятая → точка, разбор Invariant. Культура задаётся явно, текущая культура сервера не влияет (тест под ru-RU/en-US/Invariant). Номер строки считается вручную: `TextFieldParser.LineNumber` после последней строки без перевода строки равен -1 (причина бага с заголовком).
- 2026-10-20: DataAnnotations — только форма входа (`[Required]`, `[StringLength(255)]` по длине колонок, `[MinLength(1)]`); те же правила дублируются в сервисах, потому что сервисы зовёт и импорт. `PaymentEditDto` не размечен: его семантика null — задача 0018.
- 2026-10-20: `UseDeveloperExceptionPage` убран и в Development: фронтенд показывает `detail`, а страница исключения в ответ API бесполезна. В Development `detail` у 500 — текст исключения, иначе общий текст. Если клиент не принимает JSON (`Accept: text/html`), обработчик всё равно пишет `application/problem+json` — иначе middleware отдал бы пустой 500 вместо 400/404/409.
- 2026-10-20: `ExceptionHandlerMiddleware` пишет ERROR со стеком на каждое исключение, даже обработанное. `SuppressDiagnosticsCallback` есть только с .NET 10, `AddFilter`/`Logging:LogLevel` не действуют (NLog `AddNLog` убирает фильтры MEL), поэтому категория заглушена правилом в `nlog.config`; `ApiExceptionHandler` сам пишет непредвиденные ошибки как ERROR со стеком, ожидаемые — INFO без стека.
- 2026-10-20: юнит-тестов на `ApiExceptionHandler` нет (нет тестового проекта Website, маппинг тип → код тривиален); коды ответов проверены вручную через API (см. «Итог»).

## Вне объёма (найдено попутно)
- `EditPayment`: `Update` и `SetPaymentTags` — разные записи; при несуществующем теге платёж уже сохранён (`LastUpdatedAt`, описание, `excludeFromStats`), а ответ — 400. Проверка тегов до `Update` требует перенести оркестрацию в `PaymentService.Edit` + `IUnitOfWork` — задача 0018.
- `GET api/PaymentsApi/list` без `pageSize` (кнопка «Все» на «Операциях») — все 6654 операции за ~10 с (SQL-таймаута 30 с сейчас нет, ответ 200). Производительность, а не обработка ошибок — отдельная задача (например, ограничить максимум или оптимизировать выборку).
- Сумма вне диапазона `DECIMAL(18,2)` в файле импорта → 500 (переполнение в SQL, импорт откатывается). Можно проверять диапазон суммы при разборе файла → 400.
- `PUT api/PaymentSourcesApi` (legacy, id в теле) и `PUT`/`DELETE` счетов/тегов/категорий отвечают `200 Ok()` без тела, а не `204` — задача 0018 (`NoContent()`).
- Ответ `ValidationProblemDetails` (DataAnnotations) имеет английский `title` «One or more validation errors occurred.»; фронтенд показывает сообщения полей (`errors`), поэтому пользователю он не виден. При желании — `InvalidModelStateResponseFactory`.
- `backend-guide.md` §10 «Баги: `EditPayment` падает с NRE…» устарел (исправлено задачей 0013) — убрать при следующей правке техдолга.

## Итог
Сделано:
- `FinCtrl.Common.Application`: `NotFoundException`/`ValidationException`/`ConflictException`, `Paging.Validate`; `ApiExceptionHandler` + `AddProblemDetails`/`UseExceptionHandler`/`UseStatusCodePages` — все ошибки API в ProblemDetails.
- Сервисы: обязательность/длина/уникальность (счета, теги), NotFound на `GetById`/правке/удалении (счета, теги, категории, источники, платежи), проверка связей перед удалением счёта/категории (409 с количеством), существование категории/счёта/тегов/родителя до записи (400), цикл в дереве категорий и формат цвета (400), пагинация (400), группировка `NotSelected` (400).
- Импорт: `ValidationException` для формата, не-xlsx содержимого, некорректных значений с номером строки, файла без операций; CSV не зависит от культуры; CSV из одной строки-заголовка не падает.
- DataAnnotations на `AccountDto`, `TagDto`, `CategoryCrudDto`, `BulkAssignTagsDto`, `file` загрузки; проверки и `try/catch NotImplementedException` убраны из контроллеров.
- Frontend: `utils/apiError.js` (`getErrorMessage`) — alert'ы правки/создания/удаления на счетах, тегах, категориях, операциях, источниках, главной и в `TagMultiSelect` показывают `detail`/сообщения полей.
- Тесты: Domain 13, Application 91, Infrastructure 24, frontend 29 — зелёные. Новые: `AccountServiceTests`, `TagServiceTests`, `PaymentServiceTests`, сценарии ошибок в `CategoryServiceTests`/`PaymentSourceServiceTests`/`PaymentsImportServiceTests`/`MainStatisticServiceTests`, `CsvFileFormatTests` (культура, заголовок, ошибки строк; сначала красные), `XlsxFileFormatTests` (битый файл, ячейки), `FileFormatFactoryTests`, `apiError.spec.js`.
- Ручная проверка (preview backend + frontend): 7 страниц открываются, данные грузятся (операции и источники — по 500 строк), в консоли только ожидаемые 409 из сценариев. API: PUT/DELETE несуществующих id в Accounts/Tags/Categories/PaymentSources (оба PUT)/Payments → 404 ProblemDetails; неизвестный маршрут → 404 ProblemDetails; дубликат тега (в т.ч. в другом регистре) и счёта, переименование тега в занятое имя → 409; удаление счёта с 513 операциями и категории со связями → 409 с количеством; родитель — сама категория, цвет `red`, несуществующий родитель, несуществующие категория источника, категория/счёт/тег платежа, тег массового назначения → 400; пустые `tagName`/тело счёта/списки массового назначения, загрузка без файла → 400 с `errors`; `.pdf`, не-xlsx, CSV только с заголовком, CSV с некорректной суммой → 400; `GET api/PaymentsApi` без `periodType` и с `0` → 400; `page=0`/`pageSize=0` → 400; переполнение суммы в импорте → 500 ProblemDetails (и с `Accept: text/html`), в логе один ERROR со стеком. Позитивные сценарии: создание/правка/удаление тега, счёта, категории; правка источника (204) и платежа (200). UI: дубликат тега и удаление используемого счёта показывают сообщение сервера. Тестовые данные удалены, правки платежа 7948 и источника 1384 возвращены, identity-сиды Payments/PaymentSources/Tags/Accounts/Categories — 7953/1388/3/4/49.

Осталось: см. «Вне объёма».
