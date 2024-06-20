using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Payments.API.Entities
{
    public record Category
    {
        public int CategoryId { get; private set; }
        public string CategoryName { get; private set; } = string.Empty;
        public int OrderIndex { get; private set; }
        public bool IsOnlyIncome { get; private set; }
        public Category? ParentCategory { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? LastUpdatedAt { get; private set; }
    }
}
