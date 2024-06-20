using System;
using System.Diagnostics;

namespace FinCtrl.Website.BL.Data
{
    [DebuggerDisplay("{from:dd.MM.yyyy HH:mm:ss} - {to:dd.MM.yyyy HH:mm:ss}")]
    public class DatePeriod
    {
        public DatePeriod(DateTime from, DateTime to)
        {
            From = from;
            To = to;
        }

        public DateTime From { get; set; }
        /// <summary>
        /// Верхнюю границу не включаем при сравнении
        /// </summary>
        public DateTime To { get; set; }

        public bool Includes(DatePeriod period)
        {
            return period.From >= From && period.To < To;
        }

        public override string ToString()
        {
            return $"{From.ToString("dd MMMM yyyy")} - {To.ToString("dd MMMM yyyy")}";
        }

        public override bool Equals(object obj)
        {
            var period = obj as DatePeriod;
            if(period == null) return false;

            return period.From == From && period.To == To;
        }

        public override int GetHashCode()
        {
            return $"{From}-{To}".GetHashCode();
        }
    }
}
