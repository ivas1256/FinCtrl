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
        private readonly IPaymentSourceService _paymentSourceService;
        private readonly ICategoryService _categoryService;

        public PaymentService(IPaymentRepository paymentRepository, IPaymentSourceService paymentSourceService, ICategoryService categoryService)
        {
            _paymentRepository = paymentRepository;
            _paymentSourceService = paymentSourceService;
            _categoryService = categoryService;
        }

        public void Delete(params Payment[] payments)
        {
            _paymentRepository.Delete(payments);
        }

        public Payment? Get(int id)
        {
            var payment = _paymentRepository.Get(id);
            if(payment is null)
            {
                return null;
            }

            MapSourceAndCategory(new List<Payment> { payment });

            return payment;
        }

        public IEnumerable<Payment> GetAll(Pagination pagination)
        {
            var payments = _paymentRepository.GetAll(pagination);
            
            MapSourceAndCategory(payments);

            return payments;
        }

        public int Create(Payment payment)
        {
            return _paymentRepository.Create(payment);
        }

        public void Update(params Payment[] payments)
        {
            foreach (var payment in payments)
            {
                payment.Updated();
            }

            _paymentRepository.Update(payments);
        }

        public bool Exists(decimal paymentSum, DateTime paymentDate)
        {
            return _paymentRepository.Exists(paymentSum, paymentDate);
        }

        private void MapSourceAndCategory(List<Payment> payments)
        {
            var paymentSources = payments
                                .Where(x => x.PaymentSource is not null)
                                .Select(x => x.PaymentSource.PaymentSourceId)
                                .Distinct()
                                .Select(_paymentSourceService.GetById)
                            .ToDictionary(x => x.PaymentSourceId, x => x);

            var categories = _categoryService.GetAll()
                .ToDictionary(x => x.CategoryId, x => x);

            foreach (var payment in payments)
            {
                if (payment.PaymentSource is not null)
                {
                    payment.PaymentSource = paymentSources[payment.PaymentSource.PaymentSourceId];
                }

                int? currCategoryId = null;
                if (payment.PaymentCategory is not null)
                {
                    currCategoryId = payment.PaymentCategory.CategoryId;
                }
                else if (payment.PaymentSource is not null)
                {
                    currCategoryId = payment.PaymentSource.PaymentSourceCategory?.CategoryId;
                }

                if (currCategoryId is not null)
                {
                    payment.PaymentCategory = categories[currCategoryId.Value];
                }
            }
        }
    }
}
