using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Common
{
    public class DateRange : IComparable<DateRange>
    {
        public DateRange(DateTime from, DateTime to)
        {
            From = from;
            To = to;
        }
        public DateTime From { get; private set; }
        public DateTime To { get; private set; }


        public bool Intersect(DateRange other)
        {
            throw new NotImplementedException();
        }

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
