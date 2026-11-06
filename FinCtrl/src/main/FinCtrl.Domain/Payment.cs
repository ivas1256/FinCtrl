using FinCtrl.Domain.PaymentsImport;

namespace FinCtrl.Domain
{
    public class Payment
    {
        public Payment()
        {
            
        }
        public Payment(int paymentId,
            int paymentType,
            decimal paymentSum,
            DateTime paymentDate,
            string paymentDescription,
            int? paymentSourceId,
            int? paymentCategoryId,
            DateTime createdAt,
            DateTime? lastUpdatedAt)
        {
            PaymentId = paymentId;
            PaymentType = (PaymentTypeEnum)paymentType;
            PaymentSum = paymentSum;
            PaymentDate = paymentDate;
            PaymentSource = paymentSourceId is not null ? new PaymentSource(paymentSourceId.Value, null) : null;
            PaymentCategory = paymentCategoryId is not null ? new Category(paymentCategoryId.Value) : null;
            PaymentDescription = paymentDescription;
            CreatedAt = createdAt;
            LastUpdatedAt = lastUpdatedAt;
        }

        public int PaymentId { get; set; }
        public PaymentTypeEnum PaymentType { get; set; }
        public decimal PaymentSum { get; set; }
        public DateTime PaymentDate { get;  set; }
        public PaymentSource? PaymentSource { get; set; }
        public Account? Account { get; set; }
        public Category? PaymentCategory { get; set; }
        public string? PaymentDescription { get;  set; }
        public bool ExcludeFromStats { get; set; }
        public DateTime CreatedAt { get;  set; }
        public DateTime? LastUpdatedAt { get; set; }
        public List<Tag> Tags { get; set; } = new();

        /// <summary>Перевод, в который входит платёж; <c>null</c> — не связан.</summary>
        public int? TransferId { get; set; }

        /// <summary>Вторая сторона перевода; <c>null</c> — платёж не в переводе или вторая сторона не указана.</summary>
        public int? TransferPairPaymentId { get; set; }


        /// <summary>
        /// Итоговая категория: своя категория платежа приоритетнее категории его источника.
        /// </summary>
        public Category? CalculatedCategory
        {
            get
            {
                if (PaymentCategory is not null)
                    return PaymentCategory;

                if(PaymentSource?.PaymentSourceCategory is not null)
                    return PaymentSource.PaymentSourceCategory;

                return null;
            }
        }
        public void Updated(DateTime now)
        {
            LastUpdatedAt = now;
        }

        /// <summary>
        /// Приводит знак суммы к правилу «расход — отрицательная сумма»: у расхода (<see cref="PaymentTypeEnum.Spending"/>)
        /// с положительной суммой знак меняется. Нужна при чтении: в БД есть расходы, сохранённые с положительной суммой.
        /// </summary>
        public void NormalizeSumSign()
        {
            if (PaymentType == PaymentTypeEnum.Spending && PaymentSum > 0)
            {
                PaymentSum = -PaymentSum;
            }
        }

        public static Payment FromExcelPayment(ExcelPayment payment, PaymentSource paymentSource, Account? account, DateTime createdAt) => new Payment
        {
            PaymentType = payment.Sum <= 0 ? PaymentTypeEnum.Spending : PaymentTypeEnum.Income,
            PaymentSum = payment.Sum,
            PaymentDate = payment.Date,
            CreatedAt = createdAt,
            PaymentSource = paymentSource,
            Account = account
        };
    }
}
