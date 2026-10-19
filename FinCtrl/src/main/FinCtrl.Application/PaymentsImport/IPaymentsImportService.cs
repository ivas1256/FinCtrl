namespace FinCtrl.Application.PaymentsImport
{
    public interface IPaymentsImportService
    {
        Task Upload(string fileName, Stream fileStream);
    }
}