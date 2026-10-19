using System;

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
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        public void Updated()
        {
            LastUpdatedAt = DateTime.Now;
        }

        public static Account CreateFromExisting(int accountId, string excelAccountName, string displayName) => new Account
        {
            AccountId = accountId,
            ExcelAccountName = excelAccountName,
            DisplayName = displayName,
        };

        public static Account CreateNew(string excelAccountName) => new Account
        {
            ExcelAccountName = excelAccountName,
            DisplayName = excelAccountName,
            CreatedAt = DateTime.Now,
        };
    }
}
