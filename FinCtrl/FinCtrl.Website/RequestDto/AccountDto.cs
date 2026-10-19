using FinCtrl.Domain;
using System;

namespace FinCtrl.Website.RequestDto
{
    public class AccountDto
    {
        public int AccountId { get; set; }
        public string ExcelAccountName { get; set; }
        public string DisplayName { get; set; }
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
