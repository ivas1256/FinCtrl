using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.Payments
{
    internal class PaymentDto
    {
        // Payment fields
        public int PaymentId { get; set; }
        public int PaymentType { get; set; }
        public decimal PaymentSum { get; set; }
        public DateTime PaymentDate { get; set; }
        public int? PaymentSourceId { get; set; }
        public int? AccountId { get; set; }
        public int? PaymentCategoryCategoryId { get; set; }
        public string? PaymentDescription { get; set; }
        public DateTime PaymentCreatedAt { get; set; }
        public DateTime? PaymentLastUpdatedAt { get; set; }

        public bool ExcludeFromStats { get; set; }

        // Transfer fields
        public int? TransferId { get; set; }
        public int? TransferPairPaymentId { get; set; }

        // PaymentSource fields
        public int PaymentSource_PaymentSourceId { get; set; }
        public string? PaymentSource_PaymentSourceName { get; set; }
        public string? PaymentSource_Description { get; set; }
        public DateTime PaymentSource_CreatedAt { get; set; }
        public DateTime? PaymentSource_LastUpdatedAt { get; set; }
        public string? PaymentSource_Classification { get; set; }
        public bool PaymentSource_IsAiClassified { get; set; }

        // Account fields
        public int Account_AccountId { get; set; }
        public string? Account_ExcelAccountName { get; set; }
        public string? Account_DisplayName { get; set; }
        public bool Account_IsSavings { get; set; }
        public DateTime Account_CreatedAt { get; set; }
        public DateTime? Account_LastUpdatedAt { get; set; }

        // Счёт второй стороны перевода
        public int? TransferPairAccount_AccountId { get; set; }
        public string? TransferPairAccount_ExcelAccountName { get; set; }
        public string? TransferPairAccount_DisplayName { get; set; }
        public bool? TransferPairAccount_IsSavings { get; set; }
        public DateTime? TransferPairAccount_CreatedAt { get; set; }
        public DateTime? TransferPairAccount_LastUpdatedAt { get; set; }

        // Category fields
        public int Category_CategoryId { get; set; }
        public string? Category_CategoryName { get; set; }
        public int? Category_ParentCategoryId { get; set; }
        public bool Category_IsOnlyIncome { get; set; }
        public int Category_OrderIndex { get; set; }
        public DateTime Category_CreatedAt { get; set; }
        public DateTime? Category_LastUpdatedAt { get; set; }

        public int? PaymentSourceCategory_CategoryId { get; set; }
        public string? PaymentSourceCategory_CategoryName { get; set; }
        public int? PaymentSourceCategory_ParentCategoryId { get; set; }
        public bool? PaymentSourceCategory_IsOnlyIncome { get; set; }
        public int? PaymentSourceCategory_OrderIndex { get; set; }
        public DateTime? PaymentSourceCategory_CreatedAt { get; set; }
        public DateTime? PaymentSourceCategory_LastUpdatedAt { get; set; }

        public Payment MapToPayment()
        {
            var payment = new Payment
            {
                PaymentId = PaymentId,
                PaymentType = (PaymentTypeEnum)PaymentType,
                PaymentSum = PaymentSum,
                PaymentDate = PaymentDate,
                PaymentDescription = PaymentDescription,
                CreatedAt = PaymentCreatedAt,
                LastUpdatedAt = PaymentLastUpdatedAt,
                ExcludeFromStats = ExcludeFromStats,
                TransferId = TransferId,
                TransferPairPaymentId = TransferPairPaymentId,
            };

            payment.NormalizeSumSign();

            if (PaymentSourceId.HasValue)
            {
                payment.PaymentSource = new PaymentSource
                {
                    PaymentSourceId = PaymentSource_PaymentSourceId,
                    PaymentSourceName = PaymentSource_PaymentSourceName ?? string.Empty,
                    Description = PaymentSource_Description,
                    CreatedAt = PaymentSource_CreatedAt,
                    LastUpdatedAt = PaymentSource_LastUpdatedAt,
                    Classification = PaymentSource_Classification,
                    IsAiClassified = PaymentSource_IsAiClassified,
                };

                if (PaymentSourceCategory_CategoryId is not null)
                {
                    payment.PaymentSource.PaymentSourceCategory = new Category
                    {
                        CategoryId = PaymentSourceCategory_CategoryId.Value,
                        CategoryName = PaymentSourceCategory_CategoryName ?? string.Empty,
                        ParentCategoryId = PaymentSourceCategory_ParentCategoryId,
                        IsOnlyIncome = PaymentSourceCategory_IsOnlyIncome ?? false,
                        OrderIndex = PaymentSourceCategory_OrderIndex ?? 0,
                        CreatedAt = PaymentSourceCategory_CreatedAt ?? default,
                        LastUpdatedAt = PaymentSourceCategory_LastUpdatedAt,
                    };
                }
            }

            if (AccountId.HasValue)
            {
                payment.Account = new Account
                {
                    AccountId = Account_AccountId,
                    ExcelAccountName = Account_ExcelAccountName ?? string.Empty,
                    DisplayName = Account_DisplayName ?? string.Empty,
                    IsSavings = Account_IsSavings,
                    CreatedAt = Account_CreatedAt,
                    LastUpdatedAt = Account_LastUpdatedAt,
                };
            }

            if (TransferPairAccount_AccountId.HasValue)
            {
                payment.TransferPairAccount = new Account
                {
                    AccountId = TransferPairAccount_AccountId.Value,
                    ExcelAccountName = TransferPairAccount_ExcelAccountName ?? string.Empty,
                    DisplayName = TransferPairAccount_DisplayName ?? string.Empty,
                    IsSavings = TransferPairAccount_IsSavings ?? false,
                    CreatedAt = TransferPairAccount_CreatedAt ?? default,
                    LastUpdatedAt = TransferPairAccount_LastUpdatedAt,
                };
            }

            if (PaymentCategoryCategoryId.HasValue)
            {
                payment.PaymentCategory = new Category
                {
                    CategoryId = Category_CategoryId,
                    CategoryName = Category_CategoryName ?? string.Empty,
                    ParentCategoryId = Category_ParentCategoryId,
                    IsOnlyIncome = Category_IsOnlyIncome,
                    OrderIndex = Category_OrderIndex,
                    CreatedAt = Category_CreatedAt,
                    LastUpdatedAt = Category_LastUpdatedAt
                };
            }

            return payment;
        }
    }
}
