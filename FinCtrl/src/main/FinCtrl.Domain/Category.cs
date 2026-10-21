using System.Diagnostics;

namespace FinCtrl.Domain
{
    [DebuggerDisplay("{CategoryId} {CategoryName}")]
    public class Category
    {
        public Category()
        {

        }

        public Category(int categoryId)
        {
            CategoryId = categoryId;
        }

        public int CategoryId { get;  set; }
        public string CategoryName { get;  set; } = string.Empty;
        public int? ParentCategoryId { get;  set; }
        public bool IsOnlyIncome { get; set; }
        public int OrderIndex { get; set; }
        public string Color { get; set; } = DefaultColor;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        public const string DefaultColor = "#808080";

        public List<Category> Children { get; set; } = new List<Category>();

        public void Updated(DateTime now)
        {
            LastUpdatedAt = now;
        }

        public static Category CreateFakeEmpty() => new Category(int.MinValue)
        {
            CategoryName = "Без категории",
            CategoryId = int.MaxValue - 1,
            OrderIndex = int.MaxValue - 1
        };

        public static Category CreateFakeTotal() => new Category(int.MinValue)
        {
            CategoryName = "ВСЕГО потрачено:",
            CategoryId = int.MaxValue,
            OrderIndex = int.MaxValue
        };

        public bool IsIncludeInTotalResults()
        {
            //TODO maybe move to another layer

            // 33 - Накопления
            return !IsOnlyIncome 
                && CategoryId != 33
                && CategoryId != int.MaxValue - 1;
        }

        public override bool Equals(object? obj)
        {
            var category = obj as Category;
            if (category == null)
            {
                return false;
            }

            return category.CategoryId == CategoryId;
        }

        public override int GetHashCode()
        {
            return CategoryId.GetHashCode();
        }
    }
}
