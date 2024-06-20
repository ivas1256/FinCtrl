using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Payments.API.Entities
{
    public enum PaymentTypeEnum { Income, Spending, InvoiceTransfer, CardTransfer }
    public record Payment
    {
        public int PaymentId { get; private set; }
        public PaymentTypeEnum PaymentType { get; private set; }
        public decimal PaymentSum { get; private set; }
        public DateTime PaymentDate { get; private set; }
        public PaymentSource? PaymentSource { get; private set; }
        public Category? PaymentCategory { get; private set; }
        public string? PaymentDescription { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? LastUpdatedAt { get; private set; }
    }
}
