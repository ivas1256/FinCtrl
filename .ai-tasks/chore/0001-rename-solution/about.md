# [0001] Переименование солюшена в FinCtrl.Backend

- **Ветка:** `chore/0001-rename-solution`
- **Репозитории:** backend `FinCtrl/`
- **Создана:** 2026-10-19

## Контекст
Солюшен назывался `FinCtrl.Backend.Core.RestAPI.sln` — так же, как мёртвый legacy-проект `FinCtrl.Backend.Core.RestAPI/`, что путает. Старый GitHub-репозиторий `ivas1256/FinCtrl.Backend.Core.RestAPI` недоступен — нужен новый приватный.

## Постановка
- Переименовать `FinCtrl.Backend.Core.RestAPI.sln` → `FinCtrl.Backend.sln`.
- Обновить упоминания солюшена в документации (`CLAUDE.md`, `.ai-docs/`).
- Создать приватный репозиторий `ivas1256/FinCtrl.Backend`, привязать как `origin`, запушить `master` и ветку задачи, открыть PR.

## Вне объёма
- Мёртвый проект `FinCtrl.Backend.Core.RestAPI/` (папка, namespace) не переименовывается и не удаляется.
- Тестовая инфраструктура — отдельной задачей (новой функциональности здесь нет).

## Критерии приемки
- [x] В репозитории есть `FinCtrl.Backend.sln`, старого `.sln` нет
- [x] `dotnet build FinCtrl.Backend.sln` — 0 ошибок
- [x] В документации команды ссылаются на новый солюшен
- [x] Приватный репозиторий создан, `master` и ветка запушены, PR открыт
