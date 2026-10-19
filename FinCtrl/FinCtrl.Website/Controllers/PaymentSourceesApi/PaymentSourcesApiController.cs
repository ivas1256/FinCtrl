     using FinCtrl.Application.Categories;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Domain;
using FinCtrl.Website.RequestDto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinCtrl.Website.Controllers.PaymentSourceesApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentSourcesApiController : ControllerBase
    {
        private readonly IPaymentSourceService _paymentSourceService;
        private readonly ICategoryService _categoryService;

        public PaymentSourcesApiController(IPaymentSourceService paymentSourceService, ICategoryService categoryService)
        {
            _paymentSourceService = paymentSourceService;
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok((await _paymentSourceService.GetAll())
                .Select(x => PaymentSourceDto.FromModel(x)));
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetList(
            [FromQuery] string? search = null,
            [FromQuery] bool noCategoryOnly = false,
            [FromQuery] int page = 1,
            [FromQuery] int? pageSize = null)
        {
            var (sources, totalCount) = await _paymentSourceService.GetListPaged(search, noCategoryOnly, page, pageSize);
            var items = sources.Select(x => PaymentSourceDto.FromModel(x)).ToList();
            return Ok(new { Items = items, TotalCount = totalCount });
        }

        [HttpPut]
        public async Task<IActionResult> EditSources(PaymentSourceDto editDto)
        {
            var category = editDto.CategoryId.HasValue ? await _categoryService.GetById(editDto.CategoryId.Value) : null;

            var toUpdate = PaymentSource.CreateFromExisting(editDto.PaymentSourceId, editDto.PaymentSourceName, category, editDto.Description);
            await _paymentSourceService.Update(toUpdate);

            return Ok();
        }
    }
}
