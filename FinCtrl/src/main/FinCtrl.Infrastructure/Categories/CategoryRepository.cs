using FinCtrl.Application.Categories;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.Categories
{
    internal class CategoryRepository : RepositoryBase, ICategoryRepository
    {
        public CategoryRepository(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
        {
        }

        private const string _select = @"SELECT
                  c.CategoryId
                 ,c.CategoryName
                 ,c.CreatedAt
                 ,c.LastUpdatedAt
                 ,c.ParentCategoryCategoryId ParentCategoryId
                 ,c.IsOnlyIncome
                 ,c.OrderIndex
                 ,c.Color
                FROM dbo.Categories c";

        public async Task<IEnumerable<Category>> GetAll()
        {
            return await QueryCategories(_select);
        }

        public async Task<Category?> GetById(int id)
        {
            return (await QueryCategories(_select + " WHERE c.CategoryId = @id", new { id })).FirstOrDefault();
        }

        public async Task Delete(params Category[] categories)
        {
            var sql = @"DELETE FROM dbo.Categories WHERE CategoryId = @CategoryId;";

            foreach (var category in categories)
            {
                await Execute(sql, new { category.CategoryId });
            }
        }

        public async Task Update(params Category[] categories)
        {
            var sql = @"UPDATE dbo.Categories
                SET
                    CategoryName = @CategoryName,
                    LastUpdatedAt = @LastUpdatedAt,
                    ParentCategoryCategoryId = @ParentCategoryId,
                    IsOnlyIncome = @IsOnlyIncome,
                    OrderIndex = @OrderIndex,
                    Color = @Color
                WHERE
                    CategoryId = @CategoryId;";

            foreach (var category in categories)
            {
                await Execute(sql, new
                {
                    category.CategoryId,
                    category.CategoryName,
                    category.ParentCategoryId,
                    category.IsOnlyIncome,
                    category.OrderIndex,
                    category.Color,
                    category.LastUpdatedAt
                });
            }
        }

        public async Task<int> Create(Category category)
        {
            var sql = @"INSERT INTO dbo.Categories (CategoryName, ParentCategoryCategoryId, IsOnlyIncome, OrderIndex, Color, CreatedAt)
                        OUTPUT INSERTED.CategoryId
                        VALUES (@CategoryName, @ParentCategoryId, @IsOnlyIncome, @OrderIndex, @Color, @CreatedAt);";

            if (string.IsNullOrWhiteSpace(category.Color))
            {
                category.Color = Category.DefaultColor;
            }

            return await ExecuteScalar<int>(sql, new
            {
                category.CategoryName,
                category.ParentCategoryId,
                category.IsOnlyIncome,
                category.OrderIndex,
                category.Color,
                category.CreatedAt
            });
        }

        // Dapper пишет NULL из колонки Color поверх инициализатора свойства — подставляем цвет по умолчанию.
        private async Task<List<Category>> QueryCategories(string sql, object? param = null)
        {
            var categories = (await Query<Category>(sql, param)).ToList();
            foreach (var category in categories)
            {
                category.Color ??= Category.DefaultColor;
            }

            return categories;
        }
    }
}
