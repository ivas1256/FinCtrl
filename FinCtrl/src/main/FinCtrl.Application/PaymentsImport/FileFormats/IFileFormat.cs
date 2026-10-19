using FinCtrl.Domain.PaymentsImport;
using System.Collections.Generic;
using System.IO;

namespace FinCtrl.Application.PaymentsImport.FileFormats
{
    public interface IFileFormat
    {
        List<ExcelPayment> GetFileNodes(Stream fileStream);
    }
}
