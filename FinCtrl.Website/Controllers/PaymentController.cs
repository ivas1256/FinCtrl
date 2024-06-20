using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport;
using FinCtrl.Common;
using FinCtrl.DAL.Implementation;
using FinCtrl.Domain;
using FinCtrl.Website.Controllers.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace FinCtrl.Website.Controllers
{
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

        [Route("payment")]
        public IActionResult Index()
        {
            var categories = _categoryService.GetAll()
                .OrderBy(x => x.CategoryName)
                .ToDictionary(x => x.CategoryId, x => x.CategoryName);

            ViewData["categoriesList"] = categories;
            ViewBag.Items = _paymentService.GetAll(new Pagination(0, 500));

            return View();
        }

        [HttpPost("payment")]
        public IActionResult EditPayments([FromBody] List<PaymentEditDto> editorData)
        {
            var payments = new List<Payment>();
            foreach (var item in editorData)
            {
                var payment = _paymentService.Get(item.PaymentId);

                payments.Add(new Payment(
                    payment.PaymentId,
                    (int)payment.PaymentType,
                    payment.PaymentSum,
                    payment.PaymentDate,
                    item.Description,
                    payment.PaymentSource?.PaymentSourceId,
                    item.CategoryId,
                    payment.CreatedAt,
                    payment.LastUpdatedAt));
            }

            _paymentService.Update(payments.ToArray());

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("upload")]
        public IActionResult UploadFile(IFormFile file)
        {
            if (file == null)
                return RedirectToAction(nameof(Index));

            using (var stream = file.OpenReadStream())
            {
                _paymentsImportService.Upload(file.FileName, stream);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
