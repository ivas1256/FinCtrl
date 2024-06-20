using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Common
{
    public class DateRange
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
    }
}
