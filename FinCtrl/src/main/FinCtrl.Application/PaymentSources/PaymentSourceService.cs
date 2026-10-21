using FinCtrl.Domain;
using FinCtrl.Infrastructure.PaymentSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Application.PaymentSources
{
    internal class PaymentSourceService : IPaymentSourceService
    {
        private readonly IPaymentSourceRepository _repository;
        private readonly TimeProvider _timeProvider;

        public PaymentSourceService(IPaymentSourceRepository repository, TimeProvider timeProvider)
        {
            _repository = repository;
            _timeProvider = timeProvider;
        }

        // Категории источников репозиторий загружает сам (JOIN), догружать их здесь не нужно.
        public async Task<List<PaymentSource>> GetAll()
        {
            return (await _repository.GetAll())
                .OrderByDescending(x => x.PaymentSourceId)
                .ToList();
        }

        public async Task<PaymentSource?> GetById(int paymentSourceId)
        {
            return await _repository.GetById(paymentSourceId);
        }

        public async Task<PaymentSource?> GetByName(string paymentSourceName)
        {
            return await _repository.GetByName(paymentSourceName);
        }

        public async Task Delete(params PaymentSource[] id)
        {
            await _repository.Delete(id);
        }

        public async Task Edit(int paymentSourceId, string paymentSourceName, int? categoryId, string? description)
        {
            // Для UPDATE репозиторию нужен только id категории — загружать её целиком незачем.
            var category = categoryId.HasValue ? new Category(categoryId.Value) : null;
            var toUpdate = PaymentSource.CreateFromExisting(paymentSourceId, paymentSourceName, category, description);

            toUpdate.Updated(_timeProvider.GetLocalNow().DateTime);

            await _repository.Update(toUpdate);
        }

        public async Task<bool> Exists(string paymentSourceName)
        {
            return await _repository.Exists(paymentSourceName);
        }

        public async Task<int> Create(PaymentSource paymentSource)
        {
            return await _repository.Create(paymentSource);
        }

        public async Task<(List<PaymentSource> Items, int TotalCount)> GetListPaged(
            string? search, bool noCategoryOnly, int page, int? pageSize)
        {
            return await _repository.GetListPaged(search, noCategoryOnly, page, pageSize);
        }
    }
}
