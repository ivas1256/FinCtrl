namespace FinCtrl.Common
{
    /// <summary>Диапазон дат <c>[From, To)</c>: <see cref="To"/> — исключающая граница.</summary>
    public class DateRange : IComparable<DateRange>
    {
        /// <summary>
        /// Диапазон по датам «с … по …» включительно (как их выбирают в UI): время отбрасывается,
        /// <see cref="To"/> = день после <paramref name="lastDay"/>.
        /// </summary>
        public static DateRange FromInclusiveDates(DateTime firstDay, DateTime lastDay)
            => new(firstDay.Date, lastDay.Date.AddDays(1));

        public DateRange(DateTime from, DateTime to)
        {
            From = from;
            To = to;
        }
        public DateTime From { get; private set; }
        public DateTime To { get; private set; }


        public override string ToString()
        {
            return $"{From.ToString("dd MMMM yyyy")} - {To.ToString("dd MMMM yyyy")}";
        }

        public override bool Equals(object? obj)
        {
            var range = obj as DateRange;
            if (range == null)
            {
                return false;
            }

            return From.Equals(range.From) && To.Equals(range.To);
        }

        public override int GetHashCode() => From.GetHashCode() ^ To.GetHashCode();

        public int CompareTo(DateRange? other)
        {
            if (other is null)
                return 1; // Все объекты больше null

            // Сначала сравниваем начальные даты
            int fromComparison = this.From.CompareTo(other.From);
            if (fromComparison != 0)
                return fromComparison;

            // Если начальные даты равны, сравниваем конечные
            return this.To.CompareTo(other.To);
        }
    }
}
