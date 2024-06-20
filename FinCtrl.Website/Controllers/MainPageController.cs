using FinCtrl.DAL.Models;
using FinCtrl.Website.BL.Data;
using FinCtrl.Website.BL.Implementations;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using FinCtrl.Website.BL;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinCtrl.Website.Controllers
{
    public enum GroupingType { NotSelected = 0,  Month = 1, Week, Quartal, Year,  Day }

    public class MainPageController : Controller
    {
        MainStatisticCreator _statCreator;

        public MainPageController(MainStatisticCreator statCreator)
        {
            _statCreator = statCreator;
        }

        [Route("")]
        [Route("index")]
        public IActionResult Index(string periodFrom, string periodTo, GroupingType periodType)
        {
            DateTime? from = null;
            DateTime? to = null;
            DateTimeExtensions.TryParseExact(periodFrom, "dd.MM.yyyy", out from);
            DateTimeExtensions.TryParseExact(periodTo, "dd.MM.yyyy", out to);
            if (periodType == GroupingType.NotSelected)
            {
                periodType = GroupingType.Month;
            }


            var searchPeriod = new DatePeriod(DateTime.Today.AddMonths(-1).FirstDayOfMonth(), DateTime.Today.NextMonthStart());
            if (from.HasValue && to.HasValue)
            {
                searchPeriod = new DatePeriod(from.Value.Date, to.Value.Date);
            }
            ViewBag.PeriodFrom = searchPeriod.From;
            ViewBag.PeriodTo = searchPeriod.To;

            var statTable = new Dictionary<KeyValuePair<DatePeriod, Category>, MainStatisticGroupItem>();

            var items = _statCreator.Calc(searchPeriod, periodType);

            //row, col
            var table = new MainStatisticGroupItem[
                items.Select(x => x.Category).Distinct().Count(),
                items.Select(x => x.Period).Distinct().Count()
                ];

            var periods = items.Select(x => x.Period).Distinct().OrderBy(x => x.From).ToList();

            var rowIndex = 0;
            foreach (var category in items.Select(x => x.Category).Distinct().OrderBy(x => x.OrderIndex))
            {
                var categoryItems = items.Where(x => x.Category.Equals(category)).ToList();
                foreach (var categoryItem in categoryItems)
                {
                    table[rowIndex, periods.IndexOf(categoryItem.Period)] = categoryItem;
                }

                rowIndex++;
            }

            var list = new List<List<MainStatisticGroupItem>>();
            for (var i = 0; i < table.GetLength(0); i++)
            {
                var temp = new MainStatisticGroupItem[table.GetLength(1)];
                for (int n = 0; n < temp.Length; n++)
                {
                    temp[n] = table[i, n];
                }

                list.Add(temp.ToList());
            }

            ViewData["statTable"] = list;
            ViewData["columnHeaders"] = periods;

            ViewData["GroupingType"] = new List<SelectListItem>
            {
                new SelectListItem("Месяц", "1"),
                new SelectListItem("Неделя", "2"),
                new SelectListItem("Квартал", "3"),
                new SelectListItem("Год", "4"),
                new SelectListItem("День", "5"),
            };

            return View();
        }
    }

    public record GroupingTypeDto(string Text, string Value);
}
