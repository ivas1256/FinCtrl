using System.Collections.Generic;

namespace FinCtrl.Website.Controllers.PaymentsApi
{
    public class CategoryTreeDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsOnlyIncome { get; set; }
        public string Color { get; set; }
        public Dictionary<string, PeriodDto> Periods { get; set; } = new Dictionary<string, PeriodDto>();
        public List<CategoryTreeDto> Children { get; set; }
    }

}
