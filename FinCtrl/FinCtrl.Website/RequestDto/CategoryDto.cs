using FinCtrl.Domain;

namespace FinCtrl.Website.RequestDto
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsOnlyIncome { get; set; }
        public string IsOnlyIncomeStr { get; set; }
        public int OrderIndex { get; set; }

        public Category ToModel() => new Category
        {
            CategoryId = CategoryId,
            CategoryName = CategoryName,
            IsOnlyIncome = IsOnlyIncomeStr == "on",
            ParentCategoryId = ParentCategoryId,
            OrderIndex = OrderIndex
        };
    }
}
