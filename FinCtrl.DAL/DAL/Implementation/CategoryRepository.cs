using FinCtrl.DAL.Interface;
using FinCtrl.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FinCtrl.DAL.Implementation
{
    public class CategoryRepository : CRUDRepository<Category>
    {
        public DbSet<Category> Categories { get => dbContext.Categories; }

        public CategoryRepository(FinCtrlDBContext dbContext) : base(dbContext)
        {
        }

        public override Category Get(int id)
        {
            return dbContext.Categories.Include(x => x.ParentCategory).FirstOrDefault(x => x.CategoryId == id);
        }
        public List<Category> GetPlainList()
        {
            return dbContext.Categories
                .ToList();
        }

        //public override List<CategoryDTO> GetAllDTO(int pageIndex = 1, int pageSize = 100, object filter = null)
        //{
        //    var e = dbContext.Categories
        //        .Include(x => x.ParentCategory)
        //        .Include(x => x.ChildCategories)
        //        .OrderBy(x => x.CategoryId)
        //        .ToList();

        //    var res = new List<CategoryDTO>();
        //    foreach (var cat in e)
        //        res.Add(ConvertToDTO(cat));

        //    return res.Where(x => x.ParentCategory == null).ToList();
        //}

       

        //CategoryDTO ConvertToDTO(Category category)
        //{
        //    var dto = new CategoryDTO(category.ID, category.CategoryName);
        //    if (category.ParentCategory != null)
        //        dto.ParentCategory = new CategoryDTO(category.ParentCategory.ID, category.ParentCategory.CategoryName);
        //    foreach (var child in category.ChildCategories)
        //        dto.ChildCategories.Add(ConvertToDTO(child));
        //    return dto;
        //}

        //public override Category FromDTO(CategoryDTO dto)
        //{
        //    var entity = dto.CategoryId <= 0 ? new Category() : Get(dto.CategoryId);

        //    entity.CategoryName = dto.CategoryName;
        //    if (dto.ParentCategory != null)
        //        entity.ParentCategory = dbContext.Categories.Find(dto.ParentCategory.CategoryId);
        //    else
        //        entity.ParentCategory = null;

        //    return entity;
        //}
    }
}
