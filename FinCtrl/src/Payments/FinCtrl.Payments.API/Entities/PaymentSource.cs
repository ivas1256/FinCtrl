using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Payments.API.Entities
{
    public record PaymentSource
    {
        public int PaymentSourceId { get; private set; }
        public string PaymentSourceName { get; private set; } = string.Empty;
        public Category? Category { get; private set; }
        public string? Description { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime LastUpdatedAt { get; private set; }
    }
}
