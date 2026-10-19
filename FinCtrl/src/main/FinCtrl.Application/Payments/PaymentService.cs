using FinCtrl.Application.Categories;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Common;
using FinCtrl.Domain;
using FinCtrl.Infrastructure.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Application.Payments
{
    internal class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentService(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task Delete(params int[] payments)
        {
            await _paymentRepository.Delete(payments);
        }

        public async Task<Payment?> Get(int id)
        {
            return await _paymentRepository.Get(id);
        }

        public async Task<List<Payment>> GetAll(Pagination pagination)
        {
            return await _paymentRepository.GetAll(pagination);
        }

        public async Task<int> Create(Payment payment)
        {
            return await _paymentRepository.Create(payment);
        }

        public async Task Update(params Payment[] payments)
        {
            foreach (var payment in payments)
            {
                payment.Updated();
            }

           await _paymentRepository.Update(payments);
        }

        public async Task<bool> Exists(decimal paymentSum, DateTime paymentDate)
        {
            return await _paymentRepository.Exists(paymentSum, paymentDate);
        }

        public async Task<Payment?> FindDuplicate(decimal paymentSum, DateTime paymentDate)
        {
            return await _paymentRepository.FindDuplicate(paymentSum, paymentDate);
        }

        public async Task<List<Payment>> GetByDateRange(DateRange dateRange, int[]? tagIds = null)
        {
            return await _paymentRepository.GetByDateRange(dateRange, tagIds);
        }

        public async Task<List<Payment>> GetBySource(int PaymentSourceId)
        {
            return await _paymentRepository.GetBySource(PaymentSourceId);
        }

        public async Task<(List<Payment> Items, int TotalCount)> GetListPaged(
            DateRange dateRange, int? sourceId, bool noCategoryOnly,
            string sortBy, string sortDir, int page, int? pageSize, int[]? tagIds = null)
        {
            return await _paymentRepository.GetListPaged(dateRange, sourceId, noCategoryOnly, sortBy, sortDir, page, pageSize, tagIds);
        }

        public async Task SetPaymentTags(int paymentId, IReadOnlyCollection<int> tagIds)
        {
            await _paymentRepository.SetPaymentTags(paymentId, tagIds);
        }

        public async Task<int> BulkAssignTags(DateRange dateRange, List<int> categoryIds, List<int> tagIds)
        {
            var payments = await _paymentRepository.GetByDateRange(dateRange);
            var matchingIds = payments
                .Where(p => p.CalculatedCategory != null && categoryIds.Contains(p.CalculatedCategory.CategoryId))
                .Select(p => p.PaymentId)
                .ToList();

            await _paymentRepository.AddTagsToPayments(matchingIds, tagIds);

            return matchingIds.Count;
        }
    }
}
