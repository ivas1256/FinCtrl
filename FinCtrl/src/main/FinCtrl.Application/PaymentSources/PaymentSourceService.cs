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

        public PaymentSourceService(IPaymentSourceRepository repository, ICategoryService categoryService)
        {
            _repository = repository;
            _categoryService = categoryService;
        }

        public async Task<List<PaymentSource>> GetAll()
        {
            var paymentSources = (await _repository.GetAll())
                .OrderByDescending(x => x.PaymentSourceId)
                .ToList();

            MapCategories(paymentSources);

            return paymentSources;
        }

        public async Task<PaymentSource?> GetById(int paymentSourceId)
        {
            var paymentSource = await _repository.GetById(paymentSourceId);
            if (paymentSource is null)
            {
                return null;
            }

            MapCategories(new List<PaymentSource> { paymentSource });

            return paymentSource;
        }

        public async Task<PaymentSource?> GetByName(string paymentSourceName)
        {
            var paymentSource = await _repository.GetByName(paymentSourceName);
            if (paymentSource is null)
            {
                return null;
            }

            MapCategories(new List<PaymentSource> { paymentSource });

            return paymentSource;
        }

        public async Task Delete(params PaymentSource[] id)
        {
            await _repository.Delete(id);
        }

        public async Task Update(params PaymentSource[] paymentSource)
        {
            foreach (var item in paymentSource)
            {
                item.Updated();
            }

            await _repository.Update(paymentSource);
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
            var categories = paymentSources.Where(x => x.PaymentSourceCategory is not null)
                            .Select(x => x.PaymentSourceCategory.CategoryId)
                            .Distinct()
                            .Select(async x => await _categoryService.GetById(x))
                            .ToList()
                            .ToDictionary(x => x.Result.CategoryId, y => y.Result);

            foreach (var paymentSource in paymentSources)
            {
                if (paymentSource.PaymentSourceCategory is null)
                    continue;

                paymentSource.PaymentSourceCategory = categories[paymentSource.PaymentSourceCategory.CategoryId];
            }
        }
    }
}
