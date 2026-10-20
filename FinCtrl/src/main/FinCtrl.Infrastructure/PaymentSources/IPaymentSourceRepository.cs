using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.PaymentSources
{
    public interface IPaymentSourceRepository
    {
        Task<List<PaymentSource>> GetAll();
        Task<PaymentSource?> GetById(int paymentSourceId);
        Task<PaymentSource?> GetByName(string paymentSourceName);
        
        Task<int> Create(PaymentSource paymentSource);
        Task Update(params PaymentSource[] paymentSources);
        
        Task<bool> Exists(string paymentSourceName);
        Task<(List<PaymentSource> Items, int TotalCount)> GetListPaged(string? search, bool noCategoryOnly, int page, int? pageSize);
    }
}
