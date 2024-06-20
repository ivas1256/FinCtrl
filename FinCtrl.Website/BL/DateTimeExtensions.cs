using System;
using System.Globalization;

namespace FinCtrl.Website.BL
{
    public static class DateTimeExtensions
    {
        public static DateTime NextMonthStart(this DateTime date)
        {
            var t = date.AddMonths(1);
            return new DateTime(t.Year, t.Month, 1);
        }

        public static DateTime FirstDayOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        public static bool TryParseExact(string s, string format, out DateTime? date)
        {
            DateTime date1 = new DateTime();
            if (DateTime.TryParseExact(s, format, null, DateTimeStyles.None, out date1))
            {
                date = date1;
                return true;
            }
            else
            {
                date = null;
                return false;
            }
        }
    }
}
