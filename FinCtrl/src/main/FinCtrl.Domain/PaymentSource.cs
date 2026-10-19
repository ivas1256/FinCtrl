using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Domain
{
    public class PaymentSource
    {
        public PaymentSource()
        {
            
        }
        public PaymentSource(int paymentSourceId, int? categoryId)
        {
            PaymentSourceId = paymentSourceId;
            PaymentSourceCategory = categoryId is not null ? new Category(categoryId.Value) : null;
        }

        public PaymentSource(int paymentSourceId, string paymentSourceName, int? paymentSourceCategoryId, string? description, DateTime createdAt, DateTime? lastUpdatedAt)
        {
            PaymentSourceId = paymentSourceId;
            PaymentSourceName = paymentSourceName;
            PaymentSourceCategory = paymentSourceCategoryId is not null ? new Category(paymentSourceCategoryId.Value): null;
            Description = description;
            CreatedAt = createdAt;
            LastUpdatedAt = lastUpdatedAt;
        }

        public int PaymentSourceId { get; set; }
        public string PaymentSourceName { get; set; } = string.Empty;
        public Category? PaymentSourceCategory { get; set; }
        public string? Description { get;  set; }
        public DateTime CreatedAt { get;  set; }
        public DateTime? LastUpdatedAt { get;  set; }

        public void Updated()
        {
            LastUpdatedAt = DateTime.Now;
        }

        public static PaymentSource CreateFromExisting(int paymentSourceId, string paymentSourceName, Category? category, string? description)
        {
            return new PaymentSource(paymentSourceId, category?.CategoryId)
            {
                PaymentSourceName = paymentSourceName,
                Description = description,
                PaymentSourceCategory = category,
            };
        }

        public static PaymentSource CreateNew(string name) => new PaymentSource(0, null)
        {
            PaymentSourceName = name,
            CreatedAt = DateTime.Now,
        };
    }
}
