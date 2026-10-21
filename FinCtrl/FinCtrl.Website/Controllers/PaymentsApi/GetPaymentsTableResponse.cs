using FinCtrl.Common;

namespace FinCtrl.Website.Controllers.PaymentsApi
{
    public class GetPaymentsTableResponse
    {
        public List<CategoryTreeDto> PaymentsTree { get; set; } = new();
        public Dictionary<string, DateRange> Periods { get; set; } = new();
    }

}