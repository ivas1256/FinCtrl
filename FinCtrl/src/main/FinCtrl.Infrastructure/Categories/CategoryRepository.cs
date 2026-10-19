using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace FinCtrl.Infrastructure.Categories
{
    internal class CategoryRepository : RepositoryBase, ICategoryRepository
    {
        public CategoryRepository(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
        {
        }

        public async Task<IEnumerable<Category>> GetAll()
        {
            var sql = @"SELECT
                  CategoryId
                 ,CategoryName
                 ,CreatedAt
                 ,LastUpdatedAt
                 ,ParentCategoryCategoryId ParentCategoryId
                 ,IsOnlyIncome
                 ,OrderIndex
                 ,Color
                FROM dbo.Categories;";

            var conn = await _connectionFactory.GetConnectionAsync();

            return (await conn.QueryAsync(sql))
                .Select(MapCategory)
                .ToList();
        }

        public async Task<List<Category>> GetCategoriesTree()
        {
            var sql = @"
                SELECT
                  CategoryId
                 ,CategoryName
                 ,CreatedAt
                 ,LastUpdatedAt
                 ,ParentCategoryCategoryId ParentCategoryId
                 ,IsOnlyIncome
                 ,OrderIndex
                 ,Color
                FROM Categories
                WHERE ParentCategoryCategoryId IS NULL
                Order By OrderIndex;";

            var conn = await _connectionFactory.GetConnectionAsync();

            var rootCategories = (await conn.QueryAsync(sql))
                .Select(MapCategory)
                .ToList();
            foreach (var category in rootCategories)
            {
                category.Children = await GetChildCategories(category.CategoryId);
            }

            return rootCategories;
        }

        private async Task<List<Category>> GetChildCategories(int categoryId)
        {
            var sql = @"
                    SELECT
                      CategoryId
                     ,CategoryName
                     ,CreatedAt
                     ,LastUpdatedAt
                     ,ParentCategoryCategoryId ParentCategoryId
                     ,IsOnlyIncome
                     ,OrderIndex
                     ,Color
                    FROM Categories
                    WHERE ParentCategoryCategoryId = @ParentId
                    Order By OrderIndex;";

            var conn = await _connectionFactory.GetConnectionAsync();

            var children = (await conn.QueryAsync(sql, new { ParentId = categoryId }))
                .Select(MapCategory)
                .ToList();
            foreach (var childCategory in children)
            {
                childCategory.Children = await GetChildCategories(childCategory.CategoryId);
            }

            return children;
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
            var conn = await _connectionFactory.GetConnectionAsync();
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
                await conn.ExecuteAsync(sql, new
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

            category.CreatedAt = DateTime.Now;
            if (string.IsNullOrWhiteSpace(category.Color))
            {
                category.Color = Category.DefaultColor;
            }

            var conn = await _connectionFactory.GetConnectionAsync();
            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                category.CategoryName,
                category.ParentCategoryId,
                category.IsOnlyIncome,
                category.OrderIndex,
                category.Color,
                category.CreatedAt
            });
        }

        public async Task<Category?> GetById(int id)
        {
            var sql = @"SELECT
                  CategoryId
                 ,CategoryName
                 ,CreatedAt
                 ,LastUpdatedAt
                 ,ParentCategoryCategoryId ParentCategoryId
                 ,IsOnlyIncome
                 ,OrderIndex
                 ,Color
                FROM dbo.Categories
                WHERE CategoryId = @id;";

            var conn = await _connectionFactory.GetConnectionAsync();

            return (await conn.QueryAsync(sql, new { id }))
                .Select(MapCategory)
                .FirstOrDefault();
        }

        private static Category MapCategory(dynamic x) => new Category
        {
            CategoryId = x.CategoryId,
            CategoryName = x.CategoryName,
            CreatedAt = x.CreatedAt,
            LastUpdatedAt = x.LastUpdatedAt,
            ParentCategoryId = x.ParentCategoryId,
            IsOnlyIncome = x.IsOnlyIncome,
            OrderIndex = x.OrderIndex,
            Color = x.Color ?? Category.DefaultColor
        };
    }
}
