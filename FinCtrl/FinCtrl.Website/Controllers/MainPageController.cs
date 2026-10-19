using FinCtrl.Application.Categories;
using FinCtrl.Application.Statistic;
using FinCtrl.Common;
using FinCtrl.Domain;
using FinCtrl.Domain.Statistic;
using FinCtrl.Website.RequestDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace FinCtrl.Website.Controllers
{
    public class MainPageController : Controller
    {
        private readonly IMainStatisticService _mainStatisticService;
        private readonly ICategoryService _categoryService;
        private List<Category> _categoriesTree;
        private GroupingType _groupingType;

        public MainPageController(IMainStatisticService mainStatisticService, ICategoryService categoryService)
        {
            _mainStatisticService = mainStatisticService;
            _categoryService = categoryService;
        }

        [HttpGet, Route("")]
        [HttpGet, Route("index")]
        public async Task<IActionResult> Index(string periodFrom, string periodTo, GroupingType periodType)
        {
            DateTime? from = null;
            DateTime? to = null;
            DateTimeExtensions.TryParseExact(periodFrom, "dd.MM.yyyy", out from);
            DateTimeExtensions.TryParseExact(periodTo, "dd.MM.yyyy", out to);
            if (periodType == GroupingType.NotSelected)
            {
                periodType = GroupingType.Month;
            }
            _groupingType = periodType;


            var searchPeriod = new DateRange(DateTime.Today.AddMonths(-3).FirstDayOfMonth(), DateTime.Today.NextMonthStart());
            if (from.HasValue && to.HasValue)
            {
                searchPeriod = new DateRange(from.Value.Date, to.Value.Date);
            }
            ViewBag.PeriodFrom = searchPeriod.From;
            ViewBag.PeriodTo = searchPeriod.To;


            var items = await _mainStatisticService.Calculate(searchPeriod, periodType);

            _categoriesTree = await _categoryService.GetCategoriesTree();
            _categoriesTree.Add(Category.CreateFakeEmpty());
            _categoriesTree.Add(Category.CreateFakeTotal());

            var periods = items.Select(x => x.Period)
                .Distinct()
                .Order()
                .ToList();

            var result = new List<object>();
            foreach (var treeNode in _categoriesTree)
            {
                result.AddRange(BuildTree(treeNode, items, periods));
            }

            ViewBag.Periods = periods.ToDictionary(x => ToPeriodKey(x), x => x);
            ViewBag.Items = result;
            ViewBag.Categories = await _categoryService.GetAll();

            ViewData["GroupingType"] = new List<SelectListItem>
            {
                new SelectListItem("Месяц", "1"),
                new SelectListItem("Неделя", "2"),
                new SelectListItem("Квартал", "3"),
                new SelectListItem("Год", "4"),
                new SelectListItem("День", "5"),
            };

            var jso = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                WriteIndented = true
            };

            //System.IO.File.WriteAllText(@"C:\Users\Ivas1\OneDrive\Документы\categoriesTree.json", JsonSerializer.Serialize(_categoriesTree, jso));
            //System.IO.File.WriteAllText(@"C:\Users\Ivas1\OneDrive\Документы\items.json", JsonSerializer.Serialize(items, jso));
            //System.IO.File.WriteAllText(@"C:\Users\Ivas1\OneDrive\Документы\result.json", JsonSerializer.Serialize(result, jso));
            //System.IO.File.WriteAllText(@"C:\Users\Ivas1\OneDrive\Документы\periods.json", JsonSerializer.Serialize(periods, jso));

            return View();
        }

        public class CategoryTreeDto
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
            public int? ParentCategoryId { get; set; }
            public bool IsOnlyIncome { get; set; }
            public Dictionary<string, PeriodDto> Periods { get; set; } = new Dictionary<string, PeriodDto>();
            public List<CategoryTreeDto> Children { get; set; }
        }

        public class PeriodDto
        {
            public decimal TotalSum { get; set; }
            public decimal ChildrenSum { get; set; }
            public List<PaymentDto> Payments { get; set; }
        }

        public class PaymentDto
        {
            public int PaymentId { get; set; }
            public PaymentTypeEnum PaymentType { get; set; }
            public decimal PaymentSum { get; set; }
            public DateTime PaymentDate { get; set; }
            public PaymentSourceDto PaymentSource { get; set; }
            public CategoryDto Category { get; set; }
            public string PaymentDescription { get; set; }
            public bool ExcludeFromStats { get; set; }
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

                string periodKey = ToPeriodKey(period);// period.From.ToString("dd.MM.yyyy");
                if (treeNode.Periods.ContainsKey(periodKey) is false)
                    treeNode.Periods.Add(periodKey, null);

                treeNode.Periods[periodKey] = new PeriodDto
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
                        Category = _.Category == null ? null : new CategoryDto
                        {
                            CategoryId = _.Category.CategoryId,
                            CategoryName = _.Category.CategoryName,
                            IsOnlyIncome = _.Category.IsOnlyIncome
                        },
                        PaymentDescription = _.PaymentDescription
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
