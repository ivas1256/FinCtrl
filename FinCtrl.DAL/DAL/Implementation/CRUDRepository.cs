using FinCtrl.DAL.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace FinCtrl.DAL.Implementation
{
    public abstract class CRUDRepository<TEntity> : IRepository<TEntity> where TEntity : class      
    {
        public readonly FinCtrlDBContext dbContext;
        public CRUDRepository(FinCtrlDBContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public virtual TEntity FindFirst(Expression<Func<TEntity, bool>> predicte)
        {
            return dbContext.Set<TEntity>().FirstOrDefault(predicte);
        }

        public virtual TEntity CreateOrGet(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public virtual TEntity Create(TEntity entity)
        {
            dbContext.Set<TEntity>().Add(entity);
            Commit();
            return entity;
        }

        public virtual EntityEntry<TEntity> CreateNoCommit(TEntity entity)
        {
            return dbContext.Set<TEntity>().Add(entity);
        }

        public virtual TEntity Delete(int id)
        {
            var entity = dbContext.Set<TEntity>().Find(id);
            if (entity == null)
                throw new KeyNotFoundException($"Deletion Id={id} not found in DB");

            dbContext.Set<TEntity>().Remove(entity);
            Commit();
            return entity;
        }

        public virtual TEntity Get(int id)
        {            
            return dbContext.Set<TEntity>().Find(id);
        }

        public virtual List<TEntity> GetAll(int pageIndex = 0, int pageSize = 100)
        {
            //TODO: paging
            return dbContext.Set<TEntity>().ToList();
        }

        public virtual TEntity Update(TEntity entity)
        {
            dbContext.Entry(entity).State = EntityState.Modified;
            
            return entity;
        }

        public void Commit()
        {
            dbContext.SaveChanges();
        }

        public DbContext GetContext()
        {
            return dbContext;
        }
    }
}
