using FinCtrl.Common;
using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.Payments
{
    public interface IPaymentRepository
    {
        Task<List<Payment>> GetByDateRange(DateRange dateRange, int[]? tagIds = null);
        Task<Payment?> Get(int id);

        Task<int> Create(Payment payment);
        Task Update(params Payment[] payments);

        Task<Payment?> FindDuplicate(decimal paymentSum, DateTime paymentDate);
        Task<List<Payment>> GetBySource(int PaymentSourceId);
        Task<(List<Payment> Items, int TotalCount)> GetListPaged(DateRange dateRange, int? sourceId, bool noCategoryOnly, string sortBy, string sortDir, int page, int? pageSize, int[]? tagIds = null);

        Task SetPaymentTags(int paymentId, IReadOnlyCollection<int> tagIds);
        Task AddTagsToPayments(IEnumerable<int> paymentIds, IEnumerable<int> tagIds);
    }
}
