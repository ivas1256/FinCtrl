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

        public List<PaymentSource> GetAll()
        {
            var paymentSources = _repository.GetAll()
                .OrderByDescending(x => x.PaymentSourceId)
                .ToList();
            
            MapCategories(paymentSources);

            return paymentSources;
        }

        public PaymentSource? GetById(int paymentSourceId)
        {
            var paymentSource = _repository.GetById(paymentSourceId);
            if (paymentSource is null)
            {
                return null;
            }

            MapCategories(new List<PaymentSource> { paymentSource });

            return paymentSource;
        }

        public PaymentSource? GetByName(string paymentSourceName)
        {
            var paymentSource = _repository.GetByName(paymentSourceName);
            if (paymentSource is null)
            {
                return null;
            }

            MapCategories(new List<PaymentSource> { paymentSource });

            return paymentSource;
        }

        public void Delete(params PaymentSource[] id)
        {
            _repository.Delete(id);
        }

        public void Update(params PaymentSource[] paymentSource)
        {
            foreach (var item in paymentSource)
            {
                item.Updated();
            }

            _repository.Update(paymentSource);
        }

        public bool Exists(string paymentSourceName)
        {
            return _repository.Exists(paymentSourceName);
        }

        public int Create(string paymentSourceName)
        {
            return _repository.Create(paymentSourceName);
        }


        private void MapCategories(IEnumerable<PaymentSource> paymentSources)
        {
            var categories = paymentSources.Where(x => x.PaymentSourceCategory is not null)
                            .Select(x => x.PaymentSourceCategory.CategoryId)
                            .Distinct()
                            .Select(_categoryService.GetById)
                            .ToDictionary(x => x.CategoryId, y => y);

            foreach (var paymentSource in paymentSources)
            {
                if (paymentSource.PaymentSourceCategory is null)
                    continue;

                paymentSource.PaymentSourceCategory = categories[paymentSource.PaymentSourceCategory.CategoryId];
            }
        }
    }
}
