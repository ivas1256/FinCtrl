using Dapper.FluentMap.Dommel.Mapping;
using FinCtrl.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Infrastructure.Mapping
{
    internal class CategoryMap : DommelEntityMap<Category>
    {
        public CategoryMap()
        {
            ToTable("Categories", "dbo");
            Map(x => x.CategoryId).IsKey().IsIdentity();
            Map(x => x.ParentCategoryId).ToColumn("ParentCategoryCategoryId");
        }
    }
}
