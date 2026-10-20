namespace FinCtrl.Website.RequestDto
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsOnlyIncome { get; set; }
    }
}