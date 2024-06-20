using FinCtrl.Domain.PaymentsImport;
using System.Collections.Generic;
using System.IO;

namespace FinCtrl.Application.PaymentsImport.Abstractions
{
    public interface IFileFormat
    {
        static string Extension { get; }

        List<ExcelPayment> GetFileNodes(Stream fileStream);
    }
}
