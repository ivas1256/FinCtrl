using FinCtrl.Application.Categories;
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
        private readonly ICategoryService _categoryService;
        private readonly TimeProvider _timeProvider;

        public PaymentSourceService(IPaymentSourceRepository repository, ICategoryService categoryService, TimeProvider timeProvider)
        {
            _repository = repository;
            _categoryService = categoryService;
            _timeProvider = timeProvider;
        }

        public async Task<List<PaymentSource>> GetAll()
        {
            var paymentSources = (await _repository.GetAll())
                .OrderByDescending(x => x.PaymentSourceId)
                .ToList();

            await MapCategories(paymentSources);

            return paymentSources;
        }

        public async Task<PaymentSource?> GetById(int paymentSourceId)
        {
            var paymentSource = await _repository.GetById(paymentSourceId);
            if (paymentSource is null)
            {
                return null;
            }

            await MapCategories(new List<PaymentSource> { paymentSource });

            return paymentSource;
        }

        public async Task<PaymentSource?> GetByName(string paymentSourceName)
        {
            var paymentSource = await _repository.GetByName(paymentSourceName);
            if (paymentSource is null)
            {
                return null;
            }

            await MapCategories(new List<PaymentSource> { paymentSource });

            return paymentSource;
        }

        public async Task Delete(params PaymentSource[] id)
        {
            await _repository.Delete(id);
        }

        public async Task Update(params PaymentSource[] paymentSource)
        {
            var now = _timeProvider.GetLocalNow().DateTime;
            foreach (var item in paymentSource)
            {
                item.Updated(now);
            }

            await _repository.Update(paymentSource);
        }

        public async Task Edit(int paymentSourceId, string paymentSourceName, int? categoryId, string? description)
        {
            var category = categoryId.HasValue ? await _categoryService.GetById(categoryId.Value) : null;
            var toUpdate = PaymentSource.CreateFromExisting(paymentSourceId, paymentSourceName, category, description);

            await Update(toUpdate);
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


        //TODO delete this
        private async Task MapCategories(IEnumerable<PaymentSource> paymentSources)
        {
            var categoryIds = paymentSources
                .Select(x => x.PaymentSourceCategory?.CategoryId)
                .OfType<int>()
                .Distinct();

            var categories = new Dictionary<int, Category>();
            foreach (var categoryId in categoryIds)
            {
                var category = await _categoryService.GetById(categoryId);
                if (category is not null)
                    categories[categoryId] = category;
            }

            foreach (var paymentSource in paymentSources)
            {
                if (paymentSource.PaymentSourceCategory is not null
                    && categories.TryGetValue(paymentSource.PaymentSourceCategory.CategoryId, out var category))
                    paymentSource.PaymentSourceCategory = category;
            }
        }
    }
}
