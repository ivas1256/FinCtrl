using FinCtrl.Application.PaymentSources;
using FinCtrl.Website.RequestDto;
using Microsoft.AspNetCore.Mvc;

namespace FinCtrl.Website.Controllers.PaymentSourcesApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentSourcesApiController : ControllerBase
    {
        private readonly IPaymentSourceService _paymentSourceService;

        public PaymentSourcesApiController(IPaymentSourceService paymentSourceService)
        {
            _paymentSourceService = paymentSourceService;
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

        /// <summary>
        /// Правит источник платежа, id которого передан в теле (legacy-маршрут, используется массовым сохранением).
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> EditSources(PaymentSourceDto editDto)
        {
            await _paymentSourceService.Edit(editDto.PaymentSourceId, editDto.PaymentSourceName, editDto.CategoryId, editDto.Description);

            return Ok();
        }

        /// <summary>
        /// Правит один источник платежа: имя, категорию и описание.
        /// </summary>
        /// <param name="paymentSourceId">Id источника (из роута; id в теле не используется).</param>
        /// <param name="request">Новые значения полей; <c>CategoryId == null</c> — снять категорию.</param>
        [HttpPut("{paymentSourceId:int}")]
        public async Task<IActionResult> EditSource(int paymentSourceId, [FromBody] EditPaymentSourceRequest request)
        {
            await _paymentSourceService.Edit(paymentSourceId, request.PaymentSourceName, request.CategoryId, request.Description);

            return NoContent();
        }
    }
}
