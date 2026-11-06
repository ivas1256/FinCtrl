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

        /// <summary>Цвет для отображения: <see cref="DefaultColor"/>, если в БД цвет пустой.</summary>
        public string ColorOrDefault => string.IsNullOrWhiteSpace(Color) ? DefaultColor : Color;

        public List<Category> Children { get; set; } = new List<Category>();

        public void Updated(DateTime now)
        {
            LastUpdatedAt = now;
        }

        /// <summary>Фейковая категория «Без категории» для платежей без итоговой категории (сортируется перед итогом).</summary>
        public static Category CreateFakeEmpty() => new Category(CategoryIds.FakeEmpty)
        {
            CategoryName = "Без категории",
            OrderIndex = CategoryIds.FakeEmpty
        };

        /// <summary>Фейковая итоговая строка «ВСЕГО потрачено» (сортируется последней).</summary>
        public static Category CreateFakeTotal() => new Category(CategoryIds.FakeTotal)
        {
            CategoryName = "ВСЕГО потрачено:",
            OrderIndex = CategoryIds.FakeTotal
        };

        /// <summary>Фейковая категория «Переводы» для переводов между своими счетами (сортируется перед «Без категории»).</summary>
        public static Category CreateFakeTransfers() => new Category(CategoryIds.FakeTransfers)
        {
            CategoryName = "Переводы",
            OrderIndex = CategoryIds.FakeTransfers
        };

        /// <summary>Фейковая категория отчёта («Переводы», «Без категории» или итог), которой нет в БД.</summary>
        public bool IsFake => CategoryId is CategoryIds.FakeTransfers or CategoryIds.FakeEmpty or CategoryIds.FakeTotal;

        /// <summary>
        /// Входят ли платежи категории в итоги трат: не доходная, не «Накопления», не «Переводы» и не «Без категории».
        /// </summary>
        public bool IsIncludeInTotalResults()
        {
            return !IsOnlyIncome
                && CategoryId != CategoryIds.Savings
                && CategoryId != CategoryIds.FakeTransfers
                && CategoryId != CategoryIds.FakeEmpty;
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
