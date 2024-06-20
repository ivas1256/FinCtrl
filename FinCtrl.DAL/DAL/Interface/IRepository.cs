using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace FinCtrl.DAL.Interface
{
    public interface IRepository<TEntity> where TEntity : class
    {        
        TEntity FindFirst(Expression<Func<TEntity, bool>> predicte);
        TEntity CreateOrGet(TEntity entity);
        List<TEntity> GetAll(int pageIndex = 0, int pageSize = 100);        
        TEntity Get(int id);
        TEntity Create(TEntity entity);
        TEntity Update(TEntity entity);
        TEntity Delete(int id);
        EntityEntry<TEntity> CreateNoCommit(TEntity entity);
        void Commit();

        DbContext GetContext();      
    }
}
