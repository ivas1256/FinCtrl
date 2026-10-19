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
        public string PaymentDescription { get; set; }
        public DateTime PaymentCreatedAt { get; set; }
        public DateTime? PaymentLastUpdatedAt { get; set; }

        public bool ExcludeFromStats { get; set; }

        // PaymentSource fields
        public int PaymentSource_PaymentSourceId { get; set; }
        public string PaymentSource_PaymentSourceName { get; set; }
        public string PaymentSource_Description { get; set; }
        public DateTime PaymentSource_CreatedAt { get; set; }
        public DateTime? PaymentSource_LastUpdatedAt { get; set; }

        // Account fields
        public int Account_AccountId { get; set; }
        public string Account_ExcelAccountName { get; set; }
        public string Account_DisplayName { get; set; }
        public DateTime Account_CreatedAt { get; set; }
        public DateTime? Account_LastUpdatedAt { get; set; }

        // Category fields
        public int Category_CategoryId { get; set; }
        public string Category_CategoryName { get; set; }
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
            };

            if(payment.PaymentType == PaymentTypeEnum.Spending && payment.PaymentSum > 0)
            {
                payment.PaymentSum = payment.PaymentSum * -1;
            }

            if (PaymentSourceId.HasValue)
            {
                payment.PaymentSource = new PaymentSource
                {
                    PaymentSourceId = PaymentSource_PaymentSourceId,
                    PaymentSourceName = PaymentSource_PaymentSourceName,
                    Description = PaymentSource_Description,
                    CreatedAt = PaymentSource_CreatedAt,
                    LastUpdatedAt = PaymentSource_LastUpdatedAt,

                };

                if (PaymentSourceCategory_CategoryId is not null)
                {
                    payment.PaymentSource.PaymentSourceCategory = new Category
                    {
                        CategoryId = PaymentSourceCategory_CategoryId.Value,
                        CategoryName = PaymentSourceCategory_CategoryName,
                        ParentCategoryId = PaymentSourceCategory_ParentCategoryId,
                        IsOnlyIncome = PaymentSourceCategory_IsOnlyIncome.Value,
                        OrderIndex = PaymentSourceCategory_OrderIndex.Value,
                        CreatedAt = PaymentSourceCategory_CreatedAt.Value,
                        LastUpdatedAt = PaymentSourceCategory_LastUpdatedAt,
                    };
                }
            }

            if (AccountId.HasValue)
            {
                payment.Account = new Account
                {
                    AccountId = Account_AccountId,
                    ExcelAccountName = Account_ExcelAccountName,
                    DisplayName = Account_DisplayName,
                    CreatedAt = Account_CreatedAt,
                    LastUpdatedAt = Account_LastUpdatedAt,
                };
            }

            if (PaymentCategoryCategoryId.HasValue)
            {
                payment.PaymentCategory = new Category
                {
                    CategoryId = Category_CategoryId,
                    CategoryName = Category_CategoryName,
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
