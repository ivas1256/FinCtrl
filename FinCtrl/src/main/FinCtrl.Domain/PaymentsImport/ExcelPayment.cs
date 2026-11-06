namespace FinCtrl.Domain.PaymentsImport
{
    public record ExcelPayment
    {
        public ExcelPayment(DateTime date, decimal sum, string sourceName, string? accountNumber, string status, string type, string? accountName = null)
        {
            Date = date;
            Sum = sum;
            SourceName = sourceName;
            AccountNumber = accountNumber;
            Status = status;
            Type = type;
            AccountName = accountName;
        }

        public DateTime Date { get; set; }
        public decimal Sum { get; set; }
        public string SourceName { get; set; }
        /// <summary>Номер счёта (колонка D xlsx); CSV его не читает.</summary>
        public string? AccountNumber { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string? AccountName { get; set; }
    }
}
