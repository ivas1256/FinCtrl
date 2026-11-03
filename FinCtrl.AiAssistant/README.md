# FinCtrl.AiAssistant

Сервис на FastAPI: классифицирует источники платежей FinCtrl с помощью LLM (OpenAI-совместимый API).

## Запуск

```bash
python -m venv .venv
.venv\Scripts\python -m pip install -r requirements.txt
copy .env.example .env   # вписать LLM_API_KEY
.venv\Scripts\python -m uvicorn main:app --port 8000
```

Настройки — переменные окружения или `.env`: `LLM_API_KEY` (обязательна), `LLM_BASE_URL`, `LLM_MODEL`, `LOG_FILE` (по умолчанию `logs/errors.log`). Swagger — `/docs`.

## API

`POST /payment-sources/classify` — тело: `[{"Id": 250, "Name": "Cleverfish"}]`, ответ: `[{"Id": 250, "Name": "Cleverfish", "Classification": "Рыбный магазин"}]`.

- Ответ LLM проверяется: JSON, схема, Id ровно те же, что в запросе, без дублей, непустая `Classification`; `Name` возвращается из запроса.
- Пустой список → `[]` без обращения к LLM; невалидное тело → 422.
- Ошибка LLM, невалидный ответ модели, любое непредвиденное исключение → 500 `{"detail": "Внутренняя ошибка сервиса"}`, подробности с трейсом — в лог-файле.

## Структура

- `main.py` — приложение, эндпоинт, обработчики ошибок;
- `infrastructure/config.py` — настройки; `infrastructure/logging_setup.py` — лог в файл;
- `infrastructure/llm_client.py` — `LlmClient`, отправка запроса к LLM;
- `application/payment_source_classifier.py` — `PaymentSourceClassifier` (промт из `promts/`);
- `application/response_validation.py` — проверка ответа LLM.
