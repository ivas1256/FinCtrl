using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Domain
{
    public class Category
    {
        public Category()
        {
            
        }

        public Category(int categoryId)
        {
            CategoryId = categoryId;
        }

        public int CategoryId { get; private set; }
        public string CategoryName { get; private set; }
        public int? ParentCategoryId { get; private set; }
        public bool IsOnlyIncome { get; private set; }
        public int OrderIndex { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastUpdatedAt { get; private set; }

        public void Updated()
        {
            LastUpdatedAt = DateTime.Now;
        }
    }
}
