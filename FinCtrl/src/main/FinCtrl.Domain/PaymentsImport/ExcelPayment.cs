namespace FinCtrl.Domain.PaymentsImport
{
    public record ExcelPayment
    {
        public ExcelPayment(DateTime date, decimal sum, string sourceName, string invoiceNumber, string status, string type, string? accountName = null)
        {
            Date = date;
            Sum = sum;
            SourceName = sourceName;
            InvoiceNumber = invoiceNumber;
            Status = status;
            Type = type;
            AccountName = accountName;
        }

        public DateTime Date { get; set; }
        public decimal Sum { get; set; }
        public string SourceName { get; set; }
        public string InvoiceNumber { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string? AccountName { get; set; }
    }
}
