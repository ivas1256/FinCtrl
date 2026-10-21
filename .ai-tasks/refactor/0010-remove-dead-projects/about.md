# [0010] Удалить мёртвые backend-проекты

- **Ветка:** `refactor/0010-remove-dead-projects`
- **Репозитории:** backend
- **Создана:** 2026-10-20
- **Источник:** `.ai-docs/project/architecture-audit.md`, §1.1

## Контекст
В репозитории лежат 4 заброшенных проекта: попытка переписать бэкенд на EF Core. В `.sln` они не входят, рабочий код на них не ссылается. Они только путают и людей, и агентов.

## Постановка
- Удалить `FinCtrl/FinCtrl.Backend.Core.RestAPI/`, `FinCtrl/FinCtrl.DAL/`, `FinCtrl/src/Payments/`, `FinCtrl/src/FinCtrl.CoreLib/`.
- Убрать упоминания «мёртвого кода» из CLAUDE.md и project-summary.md.

## Вне объёма
- Legacy MVC внутри Website (0011).

## Критерии приемки
- [x] Каталогов нет в репозитории
- [x] `dotnet build FinCtrl.Backend.sln` проходит
- [x] CLAUDE.md/project-summary.md не упоминают удалённые проекты как существующие
- [x] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
- [x] Все тесты зелёные
