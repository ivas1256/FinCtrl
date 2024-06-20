using FinCtrl.DAL.Interface;

namespace FinCtrl.DAL.Models
{
    public class Category : ITimeLoggingEntity
    {
        public Category() { }
        public Category(int categoryId, string categoryName)
        {
            CategoryId = categoryId;
            CategoryName = categoryName;
        }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime LastUpdatedAt { get; private set; }

        public int OrderIndex { get; set; }

        public bool IsOnlyIncome { get; set; }

        public Category? ParentCategory { get; set; }

        public List<Category> ChildCategories { get; set; }

        public int ID => CategoryId;

        public override string ToString()
        {
            return $"[{ID}] {CategoryName}";
        }

        public override bool Equals(object obj)
        {
            var entity = obj as Category;
            if (entity == null)
                return false;

            return entity?.CategoryId == CategoryId;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        //TODO maybe move to another layer
        public bool IsIncludeInTotalResults()
        {
            // 33 - Накопления
            return !IsOnlyIncome && CategoryId != 33;
        }
    }
}
