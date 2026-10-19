using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport;
using FinCtrl.Common;
using FinCtrl.Domain;
using FinCtrl.Website.RequestDto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace FinCtrl.Website.Controllers
{
    [Route("[controller]")]
    public class PaymentController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IPaymentService _paymentService;

        private readonly IPaymentsImportService _paymentsImportService;


        public PaymentController(ICategoryService categoryService, IPaymentService paymentService, IPaymentsImportService paymentsImportService)
        {
            _categoryService = categoryService;
            _paymentService = paymentService;
            _paymentsImportService = paymentsImportService;
        }

        [HttpGet, Route("all")]
        public async Task<IActionResult> Index()
        {
            var categories = (await _categoryService.GetAll())
                .OrderBy(x => x.CategoryName)
                .ToDictionary(x => x.CategoryId, x => x.CategoryName);

            ViewData["categoriesList"] = categories;
            ViewBag.Items = await _paymentService.GetAll(new Pagination(0, 500));

            return View();
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(int paymentId)
        {
            await _paymentService.Delete(paymentId);
            return Ok();
        }

        [HttpPost("edit")]
        public async Task<IActionResult> EditPayment(PaymentEditDto dto)
        {
            var payment = await _paymentService.Get(dto.PaymentId);

            payment.PaymentDescription = dto.Description;
            payment.ExcludeFromStats = dto.ExcludeFromStatsStr?.Equals("on") ?? false;

            if (dto.CategoryId is not null
                && dto.CategoryId != payment.PaymentSource.PaymentSourceCategory?.CategoryId)
            {
                payment.PaymentCategory = new Category { CategoryId = dto.CategoryId.Value };
            }

            await _paymentService.Update(payment);

            return Ok();
        }

        [HttpPost("editPayments")]
        public async Task<IActionResult> EditPayments([FromBody] List<PaymentEditDto> editorData)
        {
            var payments = new List<Payment>();
            foreach (var item in editorData)
            {
                var payment = await _paymentService.Get(item.PaymentId);

                payment.PaymentDescription = item.Description;
                payment.ExcludeFromStats = item.ExcludeFromStatsStr.Equals("on");
                if (item.CategoryId is not null)
                {
                    payment.PaymentCategory = new Category { CategoryId = item.CategoryId.Value };
                }

                payments.Add(payment);
            }

            await _paymentService.Update(payments.ToArray());

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null)
                return RedirectToAction(nameof(Index));

            using (var stream = file.OpenReadStream())
            {
                await _paymentsImportService.Upload(file.FileName, stream);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
