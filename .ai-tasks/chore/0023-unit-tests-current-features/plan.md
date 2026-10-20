# План: [0023] Юнит-тесты на текущий функционал

> Статус: `запланирована`. Начинать после 0003 и 0004. При старте сверить план с текущим кодом (после 0003 сервисы используют `TimeProvider`, после 0010/0011 могла измениться структура) и обновить.

## Затрагиваемые части
- БД: нет
- Backend:
  - `FinCtrl/tests/FinCtrl.Application.Tests/` — новые тестовые классы по папкам, зеркалящим `FinCtrl.Application` (`PaymentsImport/`, `Statistic/`, `Dashboard/`, `Categories/`, `Payments/`, `PaymentSources/`, `Accounts/`, `Tags/`) + `Domain/`; фикстуры `Fixtures/*.csv`, `Fixtures/*.xlsx` (копирование в output)
  - `FinCtrl/tests/FinCtrl.Website.Tests/` — новый проект (xUnit + NSubstitute), ссылка на `FinCtrl.Website`, `InternalsVisibleTo` при необходимости
  - `FinCtrl.Backend.sln` — добавить `FinCtrl.Website.Tests`
- Frontend: `src/utils/sortUtils.spec.js`, дополнение `src/utils/heatmap.spec.js`

## Шаги
- [ ] 1. Сверить план с кодом после 0003/0004; выписать в журнал решений, какие тесты уже есть
- [ ] 2. Domain: фабрики, `FromExcelPayment`, `Updated()`, `Category.Equals`/fake-категории
- [ ] 3. `FileFormatFactory`, `CsvFileFormat`, `XlsxFileFormat` на фикстурах (фикстуры — обезличенные, 3–5 строк)
- [ ] 4. `PaymentsImportService.Upload` (моки сервисов источников/счетов/платежей и `FileFormatFactory`/`IFileFormat`)
- [ ] 5. `MainStatisticService.Calculate`
- [ ] 6. `DashboardService` (`GetSummary`, `GetChartData` × режимы)
- [ ] 7. `CategoryService`, `PaymentService`, `PaymentSourceService`, `AccountService`, `TagService`
- [ ] 8. Завести `FinCtrl.Website.Tests`, тесты `PaymentsApiController` (`EditPayment`, `Get`/`BuildTree`, `GetList`)
- [ ] 9. Frontend: `sortUtils.spec.js`, граничные случаи `heatmap.spec.js`
- [ ] 10. Обновить CLAUDE.md/регламент, если появился `FinCtrl.Website.Tests` (команды запуска)
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] PR

## Тесты
| Что тестируем | Сценарии |
|---------------|----------|
| `PaymentsImportService.Upload` | новый источник создаётся / существующий берётся; то же для счёта; «Пополнение» → Income, иначе Spending; нет дубля → `Create`; дубль без счёта → `Update` со счётом; дубль со счётом → ни `Create`, ни `Update`; поток закрывается даже при исключении |
| `FileFormatFactory` | `.xlsx`, `.csv`, регистр расширения, неизвестное расширение |
| `CsvFileFormat` / `XlsxFileFormat` | маппинг колонок, парсинг дат и сумм (разделители, знак), тип операции, имя счёта, пустые строки |
| `MainStatisticService.Calculate` | группировка День/Неделя/Месяц/Квартал/Год (квартал — фиксировать как есть, баг в 0005); границы периода; категория платежа приоритетнее категории источника; `ExcludeFromStats`; `tagIds` пробрасывается |
| `DashboardService` | итоги месяца при фиксированном «сегодня»; траты по категориям с полными именами вложенных категорий; `GetChartData` по дням/неделям/месяцам с пустыми периодами; только расходы |
| `CategoryService.GetAllChildCategories` | корень, лист, 3 уровня вложенности, несуществующий id |
| `PaymentService` / `AccountService` / `TagService` / `PaymentSourceService` | `Update` вызывает `Updated()` у каждой сущности и проброс в репозиторий; `MapCategories` подставляет категорию источнику, null-категория |
| `PaymentsApiController.EditPayment` | `CategoryId` null → не меняется; `AccountId` null/0/N; `TagIds` null/пустой/список; id из тела |
| `PaymentsApiController.BuildTree` | суммы по периодам, свёртка детей в родителя, «Без категории» и «Итого», ключи периодов и номер недели |
| `PaymentsApiController.GetList` | маппинг в `PaymentDto`, теги, итоговая категория |
| `sortUtils.js` | пустой, один элемент, дубли, обратный порядок, компаратор |
| `heatmap.js` | `maxV = 0`, отрицательные значения, пустое дерево, форматирование 0/дробных/больших сумм |

## Ручная проверка
Не требуется: поведение приложения не меняется. Проверка — `dotnet test FinCtrl.Backend.sln` и `npm run test`.

## Риски и открытые вопросы
- Известные баги (0005 квартал, 0006, 0007, 0009 `MapCategories` без `await`) могут проявиться в тестах. Решение: тест фиксирует текущее поведение с комментарием `// TODO 0005: ...` либо сценарий пропускается и отдаётся профильной fix-задаче (там тест пишется красным). `Skip =`/`.skip(` не использовать — их ловит хук.
- `PaymentSourceService.MapCategories` без `await` (0009) делает тест недетерминированным — для этого сценария тест писать в 0009, здесь — пропустить.
- Логика контроллеров переедет в сервисы в 0018. Тесты `FinCtrl.Website.Tests` тогда переносятся вместе с логикой; это ожидаемо и служит проверкой рефакторинга.
- Сервисы и контроллер могут зависеть от `internal`-типов и конкретных классов (`FileFormatFactory` без интерфейса). Если без правки кода не замокать — согласовать минимальное изменение (интерфейс/`virtual`) и записать в журнал решений.
- `XlsxFileFormat` может зависеть от лицензии/контекста библиотеки (EPPlus) — проверить при старте.
- Объём большой. Если при старте окажется слишком много для одного PR — разбить на 0023 (backend Application), отдельную задачу на `Website.Tests` и отдельную на frontend, согласовав с пользователем.

## Журнал решений
- 2026-10-20: задача заведена по запросу пользователя: зафиксировать тестами текущее поведение перед рефакторингами 0014–0018, 0021.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
