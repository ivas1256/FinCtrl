using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Common
{
    public class Pagination
    {
        public Pagination(int pageNumber = 0, int pageSize = 100)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int ToSkipAmount { get => PageNumber * PageSize; }
    }
}
