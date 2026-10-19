using FinCtrl.Application.Categories;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Domain;
using FinCtrl.Website.RequestDto;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        [HttpGet, Route("payment-source")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = (await _categoryService.GetAll())
                .OrderBy(x => x.CategoryName)
                .ToDictionary(x => x.CategoryId, x => x.CategoryName);
            ViewBag.Items = (await _paymentSourceService.GetAll()).Select(x => PaymentSourceDto.FromModel(x));

            return View();
        }

        [HttpPost("payment-source")]
        public async Task<IActionResult> EditSources([FromBody] List<PaymentSourceDto> editDtos)
        {
            var itemsToUpdate = new List<PaymentSource>();

            foreach (var item in editDtos)
            {
                var category = item.CategoryId.HasValue ? await _categoryService.GetById(item.CategoryId.Value) : null;

                itemsToUpdate.Add(PaymentSource.CreateFromExisting(item.PaymentSourceId, item.PaymentSourceName, category, item.Description));
            }

            await _paymentSourceService.Update(itemsToUpdate.ToArray());

            return RedirectToAction(nameof(Index));
        }
    }
}
