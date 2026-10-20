using FinCtrl.Application.Categories;
using FinCtrl.Application.Payments;
using FinCtrl.Application.PaymentsImport;
using FinCtrl.Application.Statistic;
using FinCtrl.Common;
using FinCtrl.Domain;
using FinCtrl.Domain.Statistic;
using FinCtrl.Website.RequestDto;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace FinCtrl.Website.Controllers.PaymentsApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsApiController : ControllerBase
    {
        private readonly IMainStatisticService _mainStatisticService;
        private readonly ICategoryService _categoryService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentsImportService _paymentsImportService;
        private List<Category> _categoriesTree = new();
        private GroupingType _groupingType;

        public PaymentsApiController(IMainStatisticService mainStatisticService, ICategoryService categoryService, IPaymentService paymentService, IPaymentsImportService paymentsImportService)
        {
            _mainStatisticService = mainStatisticService;
            _categoryService = categoryService;
            _paymentService = paymentService;
            _paymentsImportService = paymentsImportService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(GetPaymentsTableResponse), 200)]
        public async Task<IActionResult> Get(DateTime? from, DateTime? to, GroupingType periodType, [FromQuery] int[]? tagIds = null)
        {
            _groupingType = periodType;

            var searchPeriod = new DateRange(DateTime.Today.AddMonths(-3).FirstDayOfMonth(), DateTime.Today.NextMonthStart());
            if (from.HasValue && to.HasValue)
            {
                searchPeriod = new DateRange(from.Value.Date, to.Value.Date);
            }


            var items = await _mainStatisticService.Calculate(searchPeriod, periodType, tagIds);

            _categoriesTree = await _categoryService.GetCategoriesTree();
            _categoriesTree.Add(Category.CreateFakeEmpty());
            _categoriesTree.Add(Category.CreateFakeTotal());

            var periods = items.Select(x => x.Period)
                .Distinct()
                .Order()
                .ToList();

            var result = new List<CategoryTreeDto>();
            foreach (var treeNode in _categoriesTree)
            {
                result.AddRange(BuildTree(treeNode, items, periods));
            }

            return Ok(new GetPaymentsTableResponse
            {
                PaymentsTree = result,
                Periods = periods.ToDictionary(x => ToPeriodKey(x), x => x),
            });
        }


        [HttpPut("{PaymentId}")]
        public async Task<IActionResult> EditPayment([FromBody] PaymentEditDto dto)
        {
            var payment = await _paymentService.Get(dto.PaymentId);
            if (payment is null)
                return NotFound();

            payment.PaymentDescription = dto.Description;
            payment.ExcludeFromStats = dto.ExcludeFromStatsStr?.Equals("on") ?? false;

            if (dto.CategoryId is not null
                && dto.CategoryId != payment.PaymentSource?.PaymentSourceCategory?.CategoryId)
            {
                payment.PaymentCategory = new Category { CategoryId = dto.CategoryId.Value };
            }

            if (dto.AccountId is not null)
            {
                payment.Account = dto.AccountId.Value == 0 ? null : new Account { AccountId = dto.AccountId.Value };
            }

            await _paymentService.Update(payment);

            if (dto.TagIds is not null)
            {
                await _paymentService.SetPaymentTags(payment.PaymentId, dto.TagIds);
            }

            return Ok();
        }

        [HttpPost("bulk-tags")]
        public async Task<IActionResult> BulkAssignTags([FromBody] BulkAssignTagsDto dto)
        {
            var dateRange = new DateRange(dto.From.Date, dto.To.Date.AddDays(1));

            var affectedCount = await _paymentService.BulkAssignTags(dateRange, dto.CategoryIds, dto.TagIds);

            return Ok(new { AffectedCount = affectedCount });
        }

        [HttpGet("by-source/{PaymentSourceId}")]
        public async Task<IActionResult> GetBySource([FromRoute] int PaymentSourceId)
        {
            return Ok(await _paymentService.GetBySource(PaymentSourceId));
        }

        /// <summary>Импорт банковской выгрузки (xlsx/csv) одной транзакцией; отмена запроса откатывает импорт.</summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран");

            try
            {
                using var stream = file.OpenReadStream();
                await _paymentsImportService.Upload(file.FileName, stream, cancellationToken);
            }
            catch (NotImplementedException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok();
        }

        [HttpGet("list")]
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
            var dateRange = new DateRange(
                from?.Date ?? new DateTime(1753, 1, 1),
                to.HasValue ? to.Value.Date.AddDays(1) : new DateTime(9999, 12, 31)
            );

            var (payments, totalCount) = await _paymentService.GetListPaged(
                dateRange, sourceId, noCategoryOnly, sortBy, sortDir, page, pageSize, tagIds);

            var items = payments.Select(p =>
            {
                Category? effectiveCategory = null;
                string? categorySource = null;

                if (p.PaymentCategory != null)
                {
                    effectiveCategory = p.PaymentCategory;
                    categorySource = "payment";
                }
                else if (p.PaymentSource?.PaymentSourceCategory != null)
                {
                    effectiveCategory = p.PaymentSource.PaymentSourceCategory;
                    categorySource = "source";
                }

                return new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    PaymentType = p.PaymentType,
                    PaymentSum = p.PaymentSum,
                    PaymentDate = p.PaymentDate,
                    ExcludeFromStats = p.ExcludeFromStats,
                    PaymentSource = p.PaymentSource == null ? null : new PaymentSourceDto
                    {
                        PaymentSourceId = p.PaymentSource.PaymentSourceId,
                        PaymentSourceName = p.PaymentSource.PaymentSourceName,
                        CategoryId = p.PaymentSource.PaymentSourceCategory?.CategoryId ?? null
                    },
                    Account = p.Account == null ? null : AccountDto.FromModel(p.Account),
                    Category = p.PaymentCategory == null ? null : new CategoryDto
                    {
                        CategoryId = p.PaymentCategory.CategoryId,
                        CategoryName = p.PaymentCategory.CategoryName,
                        IsOnlyIncome = p.PaymentCategory.IsOnlyIncome
                    },
                    EffectiveCategory = effectiveCategory == null ? null : new CategoryDto
                    {
                        CategoryId = effectiveCategory.CategoryId,
                        CategoryName = effectiveCategory.CategoryName,
                        IsOnlyIncome = effectiveCategory.IsOnlyIncome
                    },
                    CategorySource = categorySource,
                    PaymentDescription = p.PaymentDescription,
                    Tags = p.Tags.Select(TagDto.FromModel).ToList()
                };
            }).ToList();

            return Ok(new { Items = items, TotalCount = totalCount });
        }



        private CategoryTreeDto BuildTree(Category category, List<MainStatisticGroupItem> groupItems, List<DateRange> periods)
        {
            var children = new List<CategoryTreeDto>();
            foreach (var child in category.Children)
            {
                children.Add(BuildTree(child, groupItems, periods));
            }

            var childCategories = _categoryService.GetAllChildCategories(category.CategoryId, _categoriesTree);

            var treeNode = new CategoryTreeDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                ParentCategoryId = category.ParentCategoryId,
                IsOnlyIncome = category.IsOnlyIncome,
                Color = string.IsNullOrWhiteSpace(category.Color) ? Category.DefaultColor : category.Color,
                Children = children
            };

            foreach (var period in periods)
            {
                var groupItem = groupItems
                    .Where(x => x.Category.Equals(category) && x.Period.Equals(period))
                    .FirstOrDefault();

                var periodPayments = groupItems
                    .Where(_ => childCategories.Contains(_.Category) && _.Period.Equals(period))
                    .SelectMany(x => x.Payments);
                if (groupItem is not null)
                {
                    periodPayments = periodPayments.Union(groupItem.Payments);
                }

                treeNode.Periods[ToPeriodKey(period)] = new PeriodDto
                {
                    TotalSum = (groupItem?.TotalSum ?? 0) + groupItems
                                .Where(_ => childCategories.Contains(_.Category) && _.Period.Equals(period))
                                .Sum(x => x.TotalSum),
                    Payments = periodPayments.Select(_ => new PaymentDto
                    {
                        PaymentId = _.PaymentId,
                        PaymentType = _.PaymentType,
                        PaymentSum = _.PaymentSum,
                        PaymentDate = _.PaymentDate,
                        ExcludeFromStats = _.ExcludeFromStats,
                        PaymentSource = _.PaymentSource == null ? null : new PaymentSourceDto
                        {
                            PaymentSourceId = _.PaymentSource.PaymentSourceId,
                            PaymentSourceName = _.PaymentSource.PaymentSourceName
                        },
                        Account = _.Account == null ? null : AccountDto.FromModel(_.Account),
                        Category = _.Category == null ? null : new CategoryDto
                        {
                            CategoryId = _.Category.CategoryId,
                            CategoryName = _.Category.CategoryName,
                            IsOnlyIncome = _.Category.IsOnlyIncome
                        },
                        PaymentDescription = _.PaymentDescription,
                        Tags = _.Tags.Select(TagDto.FromModel).ToList()
                    }).ToList()
                };
            }

            return treeNode;
        }

        private string ToPeriodKey(DateRange dateRange)
        {
            switch (_groupingType)
            {
                case GroupingType.Month:
                    return dateRange.From.ToString("MM yyyy").Replace(" ", "");
                case GroupingType.Week:
                    return GetWeekNumber(dateRange.From).ToString() + dateRange.From.ToString("yyyy");
                case GroupingType.Quartal:
                    return $"Q{(dateRange.From.Month - 1) / 3 + 1}{dateRange.From:yyyy}";
                case GroupingType.Year:
                    return dateRange.From.ToString("yyyy");
                case GroupingType.Day:
                    return dateRange.From.ToString("dd.MM.yyyy");
                default:
                    throw new NotImplementedException();
            }
        }

        private int GetWeekNumber(DateTime dateTime)
        {
            CultureInfo ciCurr = CultureInfo.CurrentCulture;
            int weekNum = ciCurr.Calendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return weekNum;
        }
    }

}
