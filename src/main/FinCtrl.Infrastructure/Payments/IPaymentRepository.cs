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
        List<Payment> GetAll(Pagination pagination);
        Payment? Get(int id);

        int Create(Payment payment);
        void Update(params Payment[] payments);
        void Delete(params Payment[] payments);
        
        bool Exists(decimal paymentSum, DateTime paymentDate);
    }
}
