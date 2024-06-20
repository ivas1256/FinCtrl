using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Infrastructure.Categories
{
    internal class CategoryRepository : RepositoryBase, ICategoryRepository
    {
        public CategoryRepository(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
        {
        }

        public IEnumerable<Category> GetAll()
        {
            var sql = @"SELECT
                  CategoryId
                 ,CategoryName
                 ,CreatedAt
                 ,LastUpdatedAt
                 ,ParentCategoryCategoryId
                 ,IsOnlyIncome
                 ,OrderIndex
                FROM dbo.Categories;";

            return Query<Category>(sql);
        }

        public void Delete(params Category[] categories)
        {
            foreach (var category in categories)
            {
                base.Delete(category);
            }
        }

        public void Update(params Category[] categories)
        {
            foreach (var category in categories)
            {
                base.Update(category);
            }
        }

        public Category? GetById(int id)
        {
            var sql = @"SELECT
                  CategoryId
                 ,CategoryName
                 ,CreatedAt
                 ,LastUpdatedAt
                 ,ParentCategoryCategoryId
                 ,IsOnlyIncome
                 ,OrderIndex
                FROM dbo.Categories
                WHERE CategoryId = @id;";

            return FirstOrDefault<Category>(sql, new { id });
        }
    }
}
