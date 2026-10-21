namespace FinCtrl.Common
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
    }
}
