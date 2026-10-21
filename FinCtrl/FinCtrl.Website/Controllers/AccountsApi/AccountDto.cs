using System.ComponentModel.DataAnnotations;
using FinCtrl.Domain;

namespace FinCtrl.Website.Controllers.AccountsApi
{
    public class AccountDto
    {
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Название счёта в excel-файле обязательно")]
        [StringLength(255, ErrorMessage = "Название счёта в excel-файле — не длиннее 255 символов")]
        public string ExcelAccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Отображаемое название счёта обязательно")]
        [StringLength(255, ErrorMessage = "Отображаемое название счёта — не длиннее 255 символов")]
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
