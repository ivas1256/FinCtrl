namespace FinCtrl.Application.PaymentsImport
{
    public interface IPaymentsImportService
    {
        /// <summary>
        /// Импортирует платежи из банковской выгрузки в одной транзакции: при ошибке на любой строке ничего не сохраняется.
        /// Источники и счета создаются по имени, если их нет; дубль (та же сумма и дата) не создаётся — у него только
        /// дозаполняется пустой счёт. Итог пишется в лог.
        /// </summary>
        /// <param name="fileName">Имя файла — по расширению выбирается формат.</param>
        /// <param name="fileStream">Содержимое файла; закрывается после чтения.</param>
        /// <param name="cancellationToken">Отмена проверяется перед каждой строкой; отменённый импорт откатывается.</param>
        Task Upload(string fileName, Stream fileStream, CancellationToken cancellationToken = default);
    }
}
