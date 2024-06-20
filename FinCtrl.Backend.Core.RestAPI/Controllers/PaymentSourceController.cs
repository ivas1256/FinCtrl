using FinCtrl.Backend.Core.RestAPI.Controllers.BaseControllers;
using FinCtrl.DAL.Implementation;
using FinCtrl.DAL.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinCtrl.Backend.Core.RestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentSourceController : CRUDController<PaymentSource, PaymentSourceRepository>
    {
        public PaymentSourceController(PaymentSourceRepository repository) : base(repository)
        {
        }        

        [HttpGet("set_category/{id}")]
        public void SetCategory(int id, int categoryId)
        {
            _repository.SetCategory(id, categoryId);
        }
    }
}
