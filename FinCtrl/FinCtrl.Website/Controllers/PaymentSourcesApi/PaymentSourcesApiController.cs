using FinCtrl.Application.PaymentSources;
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

        /// <summary>Все источники платежей.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PaymentSourceDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            return Ok((await _paymentSourceService.GetAll())
                .Select(PaymentSourceDto.FromModel)
                .ToList());
        }

        /// <summary>Страница источников с поиском по имени; 400 — некорректные номер или размер страницы.</summary>
        [HttpGet("list")]
        [ProducesResponseType(typeof(PagedResponse<PaymentSourceDto>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] string? search = null,
            [FromQuery] bool noCategoryOnly = false,
            [FromQuery] int page = 1,
            [FromQuery] int? pageSize = null)
        {
            var (sources, totalCount) = await _paymentSourceService.GetListPaged(search, noCategoryOnly, page, pageSize);
            var items = sources.Select(PaymentSourceDto.FromModel).ToList();
            return Ok(new PagedResponse<PaymentSourceDto>(items, totalCount));
        }

        /// <summary>
        /// Правит источник платежа, id которого передан в теле (legacy-маршрут, используется массовым сохранением).
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> EditSources(PaymentSourceDto editDto)
        {
            await _paymentSourceService.Edit(editDto.PaymentSourceId, editDto.PaymentSourceName, editDto.CategoryId, editDto.Description, editDto.Classification);

            return NoContent();
        }

        /// <summary>
        /// Правит один источник платежа: имя, категорию, описание и классификацию.
        /// </summary>
        /// <param name="paymentSourceId">Id источника (из роута; id в теле не используется).</param>
        /// <param name="request">
        /// Новые значения полей; <c>CategoryId == null</c> — снять категорию, пустая <c>Classification</c> — очистить.
        /// Изменённая классификация снимает признак ИИ-классификации.
        /// </param>
        [HttpPut("{paymentSourceId:int}")]
        public async Task<IActionResult> EditSource(int paymentSourceId, [FromBody] EditPaymentSourceRequest request)
        {
            await _paymentSourceService.Edit(paymentSourceId, request.PaymentSourceName, request.CategoryId, request.Description, request.Classification);

            return NoContent();
        }
    }
}
