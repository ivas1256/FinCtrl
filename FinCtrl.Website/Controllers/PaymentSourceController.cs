using FinCtrl.Application.Categories;
using FinCtrl.Application.PaymentSources;
using FinCtrl.DAL.Implementation;
using FinCtrl.Domain;
using FinCtrl.Website.Controllers.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace FinCtrl.Website.Controllers
{
    public class PaymentSourceController : Controller
    {
        private readonly IPaymentSourceService _paymentSourceService;
        private readonly ICategoryService _categoryService;

        public PaymentSourceController(ICategoryService categoryService, IPaymentSourceService paymentSourceService)
        {
            _categoryService = categoryService;
            _paymentSourceService = paymentSourceService;
        }

        [Route("payment-source")]
        public IActionResult Index()
        {
            var categories = _categoryService.GetAll()
                .ToDictionary(x => x.CategoryId, x => x.CategoryName);

            ViewBag.Categories = categories;
            ViewBag.Items = _paymentSourceService.GetAll();

            return View();
        }

        [HttpPost("payment-source")]
        public IActionResult EditSources([FromBody] List<PaymentSourceEditDto> editDtos)
        {
            var itemsToUpdate = new List<PaymentSource>();

            foreach (var item in editDtos)
            {
                var category = item.CategoryId.HasValue ? _categoryService.GetById(item.CategoryId.Value) : null;

                itemsToUpdate.Add(PaymentSource.CreateFromExisting(item.PaymentSourceId, item.PaymentSourceName, category, item.Description));
            }

            _paymentSourceService.Update(itemsToUpdate.ToArray());

            return RedirectToAction(nameof(Index));
        }
    }
}
