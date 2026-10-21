using FinCtrl.Common;
using FinCtrl.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Infrastructure.Payments
{
    public interface IPaymentRepository
    {
        Task<List<Payment>> GetByDateRange(DateRange dateRange, int[]? tagIds = null);
        Task<Payment?> Get(int id);

        Task<int> Create(Payment payment);
        Task Update(params Payment[] payments);

        Task<bool> Exists(decimal paymentSum, DateTime paymentDate);
        Task<Payment?> FindDuplicate(decimal paymentSum, DateTime paymentDate);
        Task<List<Payment>> GetBySource(int PaymentSourceId);
        Task<(List<Payment> Items, int TotalCount)> GetListPaged(DateRange dateRange, int? sourceId, bool noCategoryOnly, string sortBy, string sortDir, int page, int? pageSize, int[]? tagIds = null);

        Task SetPaymentTags(int paymentId, IReadOnlyCollection<int> tagIds);
        Task AddTagsToPayments(IEnumerable<int> paymentIds, IEnumerable<int> tagIds);
    }
}
