using FinCtrl.DAL.Implementation;
using FinCtrl.DAL.Interface;
using Microsoft.AspNetCore.Mvc;

namespace FinCtrl.Backend.Core.RestAPI.Controllers.BaseControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class CRUDController<TEntity, TRep> : ControllerBase        
        where TEntity : class
        where TRep : class, IRepository<TEntity>
    {
        protected readonly TRep _repository;
        public CRUDController(TRep repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public virtual List<TEntity> GetAll(int pageIndex, int pageSize, string? filter = null)
        {            
            return _repository.GetAll(pageIndex, pageSize).ToList();
        }

        [HttpGet("{id}")]
        public ActionResult<TEntity> Get(int id)
        {
            var entity = _repository.Get(id);
            if (entity == null)
                return NotFound();
            return entity;
        }

        [HttpPut("{id}")]
        public virtual ActionResult Put(int id, TEntity entity)
        {
            _repository.Update(entity);
            _repository.Commit();
            return NoContent();
        }

        [HttpPost]
        public ActionResult Post(TEntity entity)
        {
            _repository.Create(entity);
            return CreatedAtAction(nameof(Get), new {Id = -1 }, entity); //TODO fix -1
        }

        [HttpDelete("{id}")]
        public ActionResult<TEntity> Delete(int id)
        {
            var entity = _repository.Delete(id);
            return entity;
        }
    }
}
