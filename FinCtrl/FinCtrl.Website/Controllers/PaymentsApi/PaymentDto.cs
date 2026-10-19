using FinCtrl.Domain;
using FinCtrl.Website.RequestDto;
using System;
using System.Collections.Generic;

namespace FinCtrl.Website.Controllers.PaymentsApi
{
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public PaymentTypeEnum PaymentType { get; set; }
        public decimal PaymentSum { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentSourceDto PaymentSource { get; set; }
        public AccountDto Account { get; set; }
        public CategoryDto Category { get; set; }
        public CategoryDto EffectiveCategory { get; set; } // Эффективная категория (из payment или source)
        public string CategorySource { get; set; } // "payment", "source" или null
        public string PaymentDescription { get; set; }
        public bool ExcludeFromStats { get; set; }
        public List<TagDto> Tags { get; set; } = new();
    }

}
