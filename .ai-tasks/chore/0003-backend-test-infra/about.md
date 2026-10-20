# [0003] Тестовая инфраструктура backend

- **Ветка:** `chore/0003-backend-test-infra`
- **Репозитории:** backend `FinCtrl/`
- **Создана:** 2026-10-20
- **Источник:** `.ai-docs/project/architecture-audit.md`, §2.6, §2.2, §2.3

## Контекст
В backend нет ни одного теста. Все сервисы объявлены `internal`, а логика берёт время из `DateTime.Now`/`Today`, поэтому её нельзя протестировать детерминированно. Регламент (§3) требует покрывать бизнес-логику тестами, а для fix-задач сначала писать красный тест.

## Постановка
- Создать xUnit-проект `FinCtrl/tests/FinCtrl.Application.Tests/` (моки — NSubstitute) и добавить его в `FinCtrl.Backend.sln`.
- Добавить `InternalsVisibleTo` для тестового проекта (и `DynamicProxyGenAssembly2` для моков `internal`-интерфейсов) в Application (и Infrastructure, если там интерфейсы репозиториев).
- Зарегистрировать `TimeProvider.System` в DI и заменить `DateTime.Now`/`Today` в Application (`DashboardService`) и в местах создания сущностей на время из `TimeProvider` (передавать в фабрики Domain параметром).
- Написать первые тесты: `Payment.CalculatedCategory`, `Category.IsIncludeInTotalResults`, один сценарий `DashboardService` с фиксированным временем (`FakeTimeProvider` или свой стаб).

## Вне объёма
- Тесты для остальных сервисов: их пишут профильные задачи.
- `FinCtrl.Website.Tests`: заводится по необходимости в 0017/0018.

## Критерии приемки
- [ ] `dotnet test FinCtrl.Backend.sln` находит и проходит тесты
- [ ] В Domain/Application нет `DateTime.Now`/`DateTime.Today` (кроме композиции DI)
- [ ] Тест `DashboardService` не зависит от текущей даты
- [ ] Регламент/CLAUDE.md: убрано «Автотестов пока нет» для backend
- [ ] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
- [ ] Все тесты зелёные
