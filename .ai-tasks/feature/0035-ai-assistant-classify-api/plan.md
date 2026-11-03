# План: [0035] FinCtrl.AiAssistant: API классификации источников платежей

## Затрагиваемые части
- БД: нет
- Backend .NET / Frontend: нет
- `FinCtrl.AiAssistant/`: `config.py`, `models.py`, `llm_client.py`, `response_validation.py`, `classifier.py`, `logging_setup.py`, `main.py`, `requirements.txt`, `.env.example`, `.gitignore`, `README.md`
- `.claude/launch.json` — конфигурация `ai-assistant`; `CLAUDE.md`, `.ai-docs/project/project-summary.md`

## Шаги
- [x] 1. `.gitignore` (`.venv/`, `__pycache__/`, `.env`, `logs/`, `payment-sources.json`) — ключ и реальные данные не попадают в git
- [x] 2. `config.py` — настройки из env/`.env` (pydantic-settings)
- [x] 3. `models.py`, `llm_client.py` (`LlmClient`, `LlmError`)
- [x] 4. `response_validation.py` (`parse_classification`, `LlmResponseValidationError`)
- [x] 5. `classifier.py`, `logging_setup.py`, `main.py` (FastAPI, эндпоинт, обработчики ошибок)
- [x] 6. `requirements.txt`, `.env.example`, `README.md`, launch.json, документация
- [x] 7. Ручная проверка
- [ ] 8. PR

## Тесты
Автотесты не пишутся (решение пользователя). Проверка ручная.

## Ручная проверка
- импорт модуля без `LLM_API_KEY` → ошибка конфигурации;
- `parse_classification` из консоли: валидный массив, объект-обёртка, ```json-ограждение, пропущенный/лишний/дублирующийся Id, пустая классификация;
- запущенный сервис: реальный запрос → 200; пустой список → `[]`; невалидное тело → 422; неверный ключ → 500 + трейс в `logs/errors.log`.

## Риски и открытые вопросы
- Ключ лежал в коде в OneDrive — стоит перевыпустить.

## Журнал решений
- 2026-11-03: автотесты для Python не пишем — решение пользователя.
- 2026-11-03: в режиме `response_format=json_object` модель должна вернуть объект, а промт просит массив — валидатор принимает и массив, и объект с единственным полем-списком; промт не меняем.
- 2026-11-03: `Name` в ответе берётся из входных данных, а не из ответа модели — свои данные надёжнее; проверяется совпадение множества Id.
- 2026-11-03: пустой список → `[]` без вызова LLM; ошибки входной валидации — 422 (ошибка клиента), но тоже пишутся в лог как WARNING; всё остальное — 500.

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении.
