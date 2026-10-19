using FinCtrl.Backend.Core.RestAPI.Controllers.BaseControllers;
using FinCtrl.DAL.Models;
using FinCtrl.DAL.Implementation;
using Microsoft.AspNetCore.Mvc;

namespace FinCtrl.Backend.Core.RestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : CRUDController<Payment, PaymentRepository>
    {
        public PaymentController(PaymentRepository repository) : base(repository)
        {
        }
        
        [HttpGet("total-count")]
        public ActionResult<int> TotalCount()
        {
            return _repository.TotalCount();
        }
    }
}
