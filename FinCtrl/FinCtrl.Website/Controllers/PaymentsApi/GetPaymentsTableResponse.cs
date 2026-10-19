using FinCtrl.Common;
using FinCtrl.Domain;
using System.Collections.Generic;

namespace FinCtrl.Website.Controllers.PaymentsApi
{
    public class GetPaymentsTableResponse
    {
        public List<CategoryTreeDto> PaymentsTree { get; set; }
        public Dictionary<string, DateRange> Periods { get; set; }
        //public List<Category> Categories { get; set; }
    }

}
