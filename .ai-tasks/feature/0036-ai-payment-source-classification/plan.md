# План: [0036] Классификация источников платежей через AiAssistant

## Затрагиваемые части
- БД: миграция `0004_payment_source_classification.sql`
- Backend:
  - Domain: `PaymentSource` (+ `Classification`, `IsAiClassified`, правила смены классификации)
  - Application: `PaymentSources/` (правка, `PaymentSourceClassificationService`, `IPaymentSourceClassifier`, `AiAssistantOptions`), `PaymentsImport/` (результат импорта)
  - Infrastructure: `PaymentSourceRepository`/`PaymentSourceRow`, `PaymentRepository` (`SelectPayment` + `PaymentDto`), `AiAssistant/AiAssistantClient` (HttpClient)
  - Website: DTO источников, `PaymentDto`, ответ `upload`, `appsettings.json`
- Frontend: `App.vue`, `components/PaymentsImportModal.vue`, `composables/usePaymentsImport.js`, `pages/payments/Payments.vue`, `pages/paymentSources/PaymentSources.vue`, `pages/paymentsTable/PaymentSourcesList.vue` (+ `utils/groupPaymentSources.js`)

## Шаги
- [ ] 1. Миграция 0004 + накат на dev-базу
- [ ] 2. Domain/Infrastructure/Application/Website: поле классификации, правка, флаг ИИ
- [ ] 3. Классификация в выборке платежей (`SelectPayment`, `PaymentDto` ×2)
- [ ] 4. Frontend: колонка «Классификация», группировка на главной
- [ ] 5. Backend: клиент AiAssistant, сервис классификации пачками, результат импорта, ответ `upload`
- [ ] 6. Frontend: модалка импорта в меню, лоадер, вкладки; убрать загрузку с «Операций»
- [ ] Юнит-тесты (см. таблицу)
- [ ] Сборка + тесты + ручная проверка (Definition of Done)
- [ ] Документация (`project-summary.md`, `CLAUDE.md`)
- [ ] PR

## Тесты
| Что тестируем | Сценарии |
|---------------|----------|
| `PaymentSource` | ручная смена классификации сбрасывает флаг; то же значение (с пробелами) — флаг не трогает; пустая строка → `null`; ИИ-классификация ставит флаг |
| `PaymentSourceService.Edit` | классификация передаётся в Update; флаг ИИ сохраняется, если значение не менялось |
| `PaymentSourceClassificationService` | пачки по BatchSize; пустой список не зовёт клиент; ошибка клиента → текст ошибки, без исключения; пустые/лишние ответы игнорируются |
| `PaymentsImportService` | в результате только новые источники и платежи; классификация вызывается с новыми источниками |
| `AiAssistantClient` | PascalCase-запрос/ответ, ошибочный статус → исключение |
| `PaymentRepositoryTests` | алиас `PaymentSource_Classification` |
| `groupPaymentSources` (Vitest) | группировка по классификации, по имени, без источника; уникальные имена |
| `PaymentsImportModal` (Vitest) | лоадер, вкладки с данными, предупреждение классификации |

## Ручная проверка
- Накатить миграцию, запустить `ai-assistant`, `backend`, `frontend`.
- Импортировать файл через модалку → лоадер → вкладки; классификация у новых источников; в БД `IsAiClassified = 1`.
- «Источники платежей»: значок ИИ; правка классификации → значок пропал, в БД `IsAiClassified = 0`.
- Главная: группы по классификации с серым списком имён.
- Остановить `ai-assistant` → импорт другого файла: платежи сохранены, предупреждение в модалке.

## Риски и открытые вопросы
- LLM отвечает долго (десятки секунд на пачку) — таймаут HttpClient `AiAssistant:TimeoutSeconds` (120 с); импорт уже закоммичен до классификации, так что таймаут не теряет платежи.

## Журнал решений
- 2026-11-03: одна задача на оба блока — блок 2 использует поля блока 1.
- 2026-11-03: имя поля `IsAiClassified` (в постановке опечатка `IsAiClassifed`).
- 2026-11-03: AiAssistant вызывает backend (в сервисе нет CORS, ключ LLM не нужен браузеру); классификация — после коммита импорта, ошибка LLM не откатывает импорт, а возвращается в ответе `ClassificationError`.
- 2026-11-03: флаг ИИ сбрасывается только при реальном изменении значения (trim, пустая строка = очистить), чтобы массовое сохранение строки с правкой категории не снимало признак ИИ.
- 2026-11-03: в ответе `upload` новые платежи — облегчённый `ImportedPaymentDto` (данные из файла), без повторной выборки из БД.
- 2026-11-03: Обновление страниц после импорта — через общий счётчик `usePaymentsImport` (модалка живёт в `App.vue`, страницы о ней не знают).

## Вне объёма (найдено попутно)
-

## Итог
Заполняется по завершении: что сделано, что отложено.
