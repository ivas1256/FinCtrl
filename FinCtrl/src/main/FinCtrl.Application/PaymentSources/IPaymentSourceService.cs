using FinCtrl.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Application.PaymentSources
{
    public interface IPaymentSourceService
    {
        Task<List<PaymentSource>> GetAll();
        Task<PaymentSource?> GetById(int paymentSourceId);
        Task<PaymentSource?> GetByName(string paymentSourceName);

        Task<int> Create(PaymentSource paymentSource);

        /// <summary>
        /// Правит источник платежа: имя, категорию и описание.
        /// </summary>
        /// <param name="categoryId">Категория источника; <c>null</c> — снять категорию.</param>
        Task Edit(int paymentSourceId, string paymentSourceName, int? categoryId, string? description);
        Task Delete(params PaymentSource[] paymentSources);

        Task<bool> Exists(string paymentSourceName);
        Task<(List<PaymentSource> Items, int TotalCount)> GetListPaged(string? search, bool noCategoryOnly, int page, int? pageSize);
    }
}
