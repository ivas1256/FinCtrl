using FinCtrl.Domain.PaymentsImport;

namespace FinCtrl.Application.PaymentsImport.FileFormats
{
    public interface IFileFormat
    {
        List<ExcelPayment> GetFileNodes(Stream fileStream);
    }
}
