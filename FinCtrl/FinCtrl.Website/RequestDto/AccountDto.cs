using FinCtrl.Domain;

namespace FinCtrl.Website.RequestDto
{
    public class AccountDto
    {
        public int AccountId { get; set; }
        public string ExcelAccountName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        public static AccountDto FromModel(Account account) => new AccountDto
        {
            AccountId = account.AccountId,
            ExcelAccountName = account.ExcelAccountName,
            DisplayName = account.DisplayName,
            CreatedAt = account.CreatedAt,
            LastUpdatedAt = account.LastUpdatedAt
        };
    }
}