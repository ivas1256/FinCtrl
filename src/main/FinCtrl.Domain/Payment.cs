using FinCtrl.Domain.PaymentsImport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Domain
{
    public record Payment
    {
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

        public int PaymentId { get; private set; }
        public PaymentTypeEnum PaymentType { get; private set; }
        public decimal PaymentSum { get; private set; }
        public DateTime PaymentDate { get; private set; }
        public PaymentSource? PaymentSource { get; set; }
        public Category? PaymentCategory { get; set; }
        public string? PaymentDescription { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastUpdatedAt { get; private set; }

        public void Updated()
        {
            LastUpdatedAt = DateTime.Now;
        }

        public static Payment FromExcelPayment(ExcelPayment payment, PaymentSource paymentSource)
        {
            return new Payment(
                0,
                payment.Sum <= 0 ? (int)PaymentTypeEnum.Spending : (int)PaymentTypeEnum.Income,
                payment.Sum,
                payment.Date,
                null,
                null,
                null,
                DateTime.Now,
                null
                )
            {
                PaymentSource = paymentSource
            };
        }
    }
}
