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

        /// <summary>Накопительный счёт (вклад): перевод на него модалка правки платежа относит к «Накоплениям».</summary>
        public bool IsSavings { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        public void Updated(DateTime now)
        {
            LastUpdatedAt = now;
        }

        public static Account CreateFromExisting(int accountId, string excelAccountName, string displayName, bool isSavings) => new Account
        {
            AccountId = accountId,
            ExcelAccountName = excelAccountName,
            DisplayName = displayName,
            IsSavings = isSavings,
        };

        public static Account CreateNew(string excelAccountName, DateTime createdAt) => new Account
        {
            ExcelAccountName = excelAccountName,
            DisplayName = excelAccountName,
            CreatedAt = createdAt,
        };
    }
}
