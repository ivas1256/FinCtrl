using FinCtrl.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Application.Categories
{
    public interface ICategoryService
    {
        Category GetById(int id);

        IEnumerable<Category> GetAll();

        void Update(params Category[] categories);

        void Delete(params Category[] categories);
    }
}
