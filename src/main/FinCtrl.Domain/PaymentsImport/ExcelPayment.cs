using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Domain.PaymentsImport
{
    public record ExcelPayment
    {
        public ExcelPayment(DateTime date, decimal sum, string sourceName, string invoiceNumber, string status)
        {
            Date = date;
            Sum = sum;
            SourceName = sourceName;
            InvoiceNumber = invoiceNumber;
            Status = status;
        }

        public DateTime Date { get; set; }
        public decimal Sum { get; set; }
        public string SourceName { get; set; }
        public string InvoiceNumber { get; set; }
        public string Status { get; set; }
    }
}
