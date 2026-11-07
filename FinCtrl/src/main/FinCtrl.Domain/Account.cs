namespace FinCtrl.Domain
{
    public class Account
    {
        public Account()
        {

        }

        public Account(int accountId, string excelAccountName, string displayName, DateTime createdAt, DateTime? lastUpdatedAt)
        {
            AccountId = accountId;
            ExcelAccountName = excelAccountName;
            DisplayName = displayName;
            CreatedAt = createdAt;
            LastUpdatedAt = lastUpdatedAt;
        }

        public int AccountId { get; set; }
        public string ExcelAccountName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Номер счёта из банковского файла (колонка D xlsx) — ключ сопоставления при импорте; <c>null</c> — неизвестен.</summary>
        public string? AccountNumber { get; set; }

        /// <summary>Накопительный счёт (вклад): перевод на него модалка правки платежа относит к «Накоплениям».</summary>
        public bool IsSavings { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        public void Updated(DateTime now)
        {
            LastUpdatedAt = now;
        }

        /// <summary>Номер счёта без пробелов по краям; пустой — <c>null</c>.</summary>
        public static string? NormalizeAccountNumber(string? accountNumber) =>
            string.IsNullOrWhiteSpace(accountNumber) ? null : accountNumber.Trim();

        public static Account CreateFromExisting(int accountId, string excelAccountName, string displayName, bool isSavings, string? accountNumber = null) => new Account
        {
            AccountId = accountId,
            ExcelAccountName = excelAccountName,
            DisplayName = displayName,
            IsSavings = isSavings,
            AccountNumber = NormalizeAccountNumber(accountNumber),
        };

        public static Account CreateNew(string excelAccountName, DateTime createdAt, string? accountNumber = null) => new Account
        {
            ExcelAccountName = excelAccountName,
            DisplayName = excelAccountName,
            AccountNumber = NormalizeAccountNumber(accountNumber),
            CreatedAt = createdAt,
        };
    }
}
