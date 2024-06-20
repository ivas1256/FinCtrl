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
        List<PaymentSource> GetAll();
        PaymentSource? GetById(int paymentSourceId);
        PaymentSource? GetByName(string paymentSourceName);

        int Create(string paymentSourceName);
        void Update(params PaymentSource[] paymentSources);
        void Delete(params PaymentSource[] paymentSources);
        
        bool Exists(string paymentSourceName);
    }
}
