using System.ComponentModel.DataAnnotations;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport;
using FinCtrl.Application.Statistic;
using FinCtrl.Common;
using FinCtrl.Domain.Statistic;
using Microsoft.AspNetCore.Mvc;

namespace FinCtrl.Website.Controllers.PaymentsApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsApiController : ControllerBase
    {
        private readonly IMainStatisticService _mainStatisticService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentsImportService _paymentsImportService;

        public PaymentsApiController(IMainStatisticService mainStatisticService, IPaymentService paymentService, IPaymentsImportService paymentsImportService)
        {
            _mainStatisticService = mainStatisticService;
            _paymentService = paymentService;
            _paymentsImportService = paymentsImportService;
        }

        /// <summary>
        /// Таблица главной: дерево категорий × периоды группировки. Период — с <paramref name="from"/> по <paramref name="to"/>
        /// включительно; если не задана хотя бы одна дата — с начала месяца три месяца назад до конца текущего месяца.
        /// 400 — не выбран тип группировки.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(GetPaymentsTableResponse), 200)]
        public async Task<IActionResult> Get(DateTime? from, DateTime? to, GroupingType periodType, [FromQuery] int[]? tagIds = null)
        {
            var dateRange = from.HasValue && to.HasValue ? DateRange.FromInclusiveDates(from.Value, to.Value) : null;

            var table = await _mainStatisticService.GetPaymentsTable(dateRange, periodType, tagIds);

            return Ok(GetPaymentsTableResponse.FromModel(table));
        }

        /// <summary>
        /// Правит платёж и его теги одной транзакцией; поля со значением <c>null</c> не меняются.
        /// 404 — платёж не найден, 400 — категория, счёт или тег не найдены (платёж при этом не меняется).
        /// </summary>
        [HttpPut("{paymentId:int}")]
        public async Task<IActionResult> EditPayment(int paymentId, [FromBody] EditPaymentRequest request)
        {
            await _paymentService.Edit(request.ToModel(paymentId));

            return NoContent();
        }

        /// <summary>
        /// Добавляет теги платежам с <c>From</c> по <c>To</c> включительно, у которых итоговая категория из списка;
        /// пустой <c>CategoryIds</c> — всем платежам периода, включая без категории.
        /// 400 — не выбраны теги, тег не найден или период перевёрнут.
        /// </summary>
        [HttpPost("bulk-tags")]
        [ProducesResponseType(typeof(BulkAssignTagsResponse), 200)]
        public async Task<IActionResult> BulkAssignTags([FromBody] BulkAssignTagsRequest request)
        {
            var dateRange = DateRange.FromInclusiveDates(request.From, request.To);

            var affectedCount = await _paymentService.BulkAssignTags(dateRange, request.CategoryIds, request.TagIds);

            return Ok(new BulkAssignTagsResponse(affectedCount));
        }

        /// <summary>Платежи источника.</summary>
        [HttpGet("by-source/{paymentSourceId}")]
        [ProducesResponseType(typeof(List<PaymentDto>), 200)]
        public async Task<IActionResult> GetBySource(int paymentSourceId)
        {
            var payments = await _paymentService.GetBySource(paymentSourceId);

            return Ok(payments.Select(PaymentDto.FromModel).ToList());
        }

        /// <summary>
        /// Импорт банковской выгрузки (xlsx/csv) одной транзакцией; отмена запроса откатывает импорт.
        /// 400 — файл не выбран, формат не поддерживается, файл не читается или в нём нет операций.
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([Required(ErrorMessage = "Файл не выбран")] IFormFile file, CancellationToken cancellationToken)
        {
            using var stream = file.OpenReadStream();
            await _paymentsImportService.Upload(file.FileName, stream, cancellationToken);

            return Ok();
        }

        /// <summary>
        /// Страница списка операций. <paramref name="from"/>/<paramref name="to"/> — первый и последний день
        /// (включительно), не заданы — без границы. 400 — некорректные номер или размер страницы.
        /// </summary>
        [HttpGet("list")]
        [ProducesResponseType(typeof(PagedResponse<PaymentDto>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? sourceId = null,
            [FromQuery] bool noCategoryOnly = false,
            [FromQuery] string sortBy = "date",
            [FromQuery] string sortDir = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int? pageSize = null,
            [FromQuery] int[]? tagIds = null)
        {
            var (payments, totalCount) = await _paymentService.GetListPaged(
                from?.Date, to?.Date.AddDays(1), sourceId, noCategoryOnly, sortBy, sortDir, page, pageSize, tagIds);

            return Ok(new PagedResponse<PaymentDto>(payments.Select(PaymentDto.FromModel).ToList(), totalCount));
        }
    }
}
