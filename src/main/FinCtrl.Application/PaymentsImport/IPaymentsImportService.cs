namespace FinCtrl.Application.PaymentsImport
{
    public interface IPaymentsImportService
    {
        void Upload(string fileName, Stream fileStream);
    }
}