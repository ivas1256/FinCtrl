# План: [0003] Тестовая инфраструктура backend

> Статус: `в работе`.

## Затрагиваемые части
- `FinCtrl/tests/FinCtrl.Application.Tests/*`, `FinCtrl/tests/FinCtrl.Domain.Tests/*`
- `FinCtrl.Backend.sln`
- `src/main/FinCtrl.Application/FinCtrl.Application.csproj` (`Microsoft.Bcl.TimeProvider`, `InternalsVisibleTo`), `ServiceCollectionExtensions.cs`
- Domain: `Account.cs`, `Category.cs`, `Payment.cs`, `PaymentSource.cs`, `Tag.cs`
- Application: сервисы `Account`/`Category`/`Payment`/`PaymentSource`/`Tag`/`Dashboard`/`PaymentsImport`
- Infrastructure: `Categories/CategoryRepository.cs` (`CreatedAt`)
- Website: `AccountsApiController`, `TagsApiController` (вызывают `CreateNew`)

## Шаги
- [x] Создать тестовые проекты, подключить в .sln (папка решения `tests`)
- [x] InternalsVisibleTo
- [x] TimeProvider в DI, убрать DateTime.Now/Today из Domain/Application
- [x] Первые тесты
- [x] Обновить CLAUDE.md (раздел «Команды»), project-summary.md
- [x] Сборка + тесты (Definition of Done)
- [ ] PR

## Тесты
- `FinCtrl.Domain.Tests/PaymentTests`: `CalculatedCategory` (своя категория приоритетнее категории источника, fallback на источник, null), `Updated(now)`.
- `FinCtrl.Domain.Tests/CategoryTests`: `IsIncludeInTotalResults` (обычная, «только доход», накопления 33, фейковая «Без категории»).
- `FinCtrl.Application.Tests/Dashboard/DashboardServiceTests`: периоды (сегодня/вчера/неделя/месяц, диапазон запроса) и суммы при фиксированном времени `FakeTimeProvider` — не зависят от текущей даты.

## Ручная проверка
Поведение не меняется (время по-прежнему локальное, `TimeProvider.System.GetLocalNow()`). Проверка: сборка `FinCtrl.Website` без ошибок, DI собирается при старте. Preview `backend` не запускался: порт/бинарники заняты уже запущенным пользователем экземпляром `FinCtrl.Website`.

## Риски и открытые вопросы
- Приложения на net7 не имеют встроенного `TimeProvider` → взят пакет `Microsoft.Bcl.TimeProvider` 8.0.1 (на net9 в Website он сводится к встроенному типу).

## Журнал решений
- 2026-10-20: задача заведена по итогам аудита архитектуры (`.ai-docs/project/architecture-audit.md`).
- 2026-10-20: тесты Domain вынесены в отдельный проект `FinCtrl.Domain.Tests` — по backend-guide §8 «отдельный проект на каждый тестируемый проект».
- 2026-10-20: время передаётся в сущности параметром (`Updated(DateTime now)`, `CreateNew(..., DateTime createdAt)`), `TimeProvider` — только в Application/Website. `AccountsApiController`/`TagsApiController` получили `TimeProvider` в конструктор, т.к. сами вызывают `CreateNew` (перенос создания в сервис — задача 0018).
- 2026-10-20: хук требует 0 предупреждений во всех изменённых файлах, поэтому попутно исправлены предупреждения в них: `Category.CategoryName = string.Empty`, `ICategoryService.GetById` → `Task<Category?>`, `FindCategoryWithChildren` → `Category?`, null-проверки в `PaymentsImportService.GetOrCreate*`, `MapCategories` вызывается с `await` и без `.Result` (поведение при ненайденной категории сохранено — остаётся категория с одним id). Полная замена `MapCategories` на JOIN остаётся в задаче 0009.

## Вне объёма (найдено попутно)
- `DateTime.Today` остаётся в Website: `DashboardApiController`, `PaymentsApiController`, `MainPageController` — уйдёт вместе с логикой периодов в 0018 (и 0011 для MVC).

## Итог
Заполняется по завершении.
