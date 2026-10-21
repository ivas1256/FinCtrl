# [0011] Удалить legacy MVC-фронтенд, оставить только REST API

- **Ветка:** `refactor/0011-remove-legacy-mvc`
- **Репозитории:** backend
- **Создана:** 2026-10-20
- **Источник:** `.ai-docs/project/architecture-audit.md`, §1.2

## Контекст
UI полностью работает на Vue, а MVC/Razor-фронтенд остался в `FinCtrl.Website` и всё ещё подключён. `GET /` отдаёт Razor-страницу. Пишущие endpoint'ы (`DELETE /Payment/delete`, `POST /Payment/upload|edit|editPayments`, `POST /Category/update`, `POST /payment-source`) доступны в обход SPA и без antiforgery. Вместе с MVC в Website лежит около 14 МБ `wwwroot/lib`, копия `BuildTree` и члены сервисов, которые используются только из MVC.

## Постановка
- Удалить `Controllers/MainPageController.cs`, `PaymentController.cs`, `PaymentSourceController.cs`, `CategoryController.cs`, весь `Views/`, `Models/`, `wwwroot/`, `jsconfig.json`, пустые `<Folder>` в csproj.
- `Startup`: `AddControllersWithViews` → `AddControllers`, `MapControllerRoute` → `MapControllers`, убрать `UseStaticFiles`.
- Удалить код, нужный только MVC: `IPaymentService/IPaymentRepository.GetAll(Pagination)`, `IPaymentService.Delete` (+ репозиторий, если не используется), `Pagination`, `CategoryDto.ToModel`/`IsOnlyIncomeStr` и неиспользуемые поля, `DateTimeExtensions.TryParseExact` (если без ссылок), пакет `Microsoft.VisualStudio.Web.CodeGeneration.Design`.
- Обновить CLAUDE.md и project-summary: в Website только REST API.

## Вне объёма
- Прочая чистка мусора (0012).
- Изменение URL `/api/*`.

## Критерии приемки
- [x] В `FinCtrl.Website/Controllers` нет контроллеров, кроме `*ApiController`; нет `Views/`, `wwwroot/`, `Models/`
- [x] `GET /` на :5000 → 404; Swagger (`/swagger`) работает
- [x] Все страницы SPA (главная, Dashboard, Операции, Источники, Категории, Счета, Теги) работают, загрузка файла работает, в консоли и логах нет ошибок
- [x] Нет членов сервисов/репозиториев без использования, оставшихся после удаления MVC
- [x] Сборка затронутых частей проходит без ошибок, нет новых предупреждений в изменённых файлах
- [x] Все тесты зелёные
