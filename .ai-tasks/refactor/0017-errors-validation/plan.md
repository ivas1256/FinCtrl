# План: [0017] Обработка ошибок и валидация по гайду

> Статус: `в работе`. План сверен с кодом после задач 0005–0016 (интерфейсы репозиториев в Application, async-репозитории, `IUnitOfWork`, Nullable в Website).

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
- [ ] Исключения (`FinCtrl.Common.Application`)
- [ ] Глобальный обработчик (ProblemDetails 400/404/409/500)
- [ ] Валидация в сервисах (обязательность, уникальность, существование связанных сущностей; NotFound на правке/удалении)
- [ ] Проверка связей перед Delete (счёт, категория)
- [ ] Импорт: формат файла, ошибки разбора, культура CSV, CSV только с заголовком
- [ ] Группировка `NotSelected` → 400
- [ ] DataAnnotations
- [ ] Frontend
- [ ] Тесты и ручная проверка
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

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

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
