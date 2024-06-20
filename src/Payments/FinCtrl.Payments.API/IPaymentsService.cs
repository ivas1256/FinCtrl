using FinCtrl.Common;
using FinCtrl.Payments.API.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Payments.API
{
    public interface IPaymentsService
    {
        int Create(Payment payment);
        void Delete(Payment payment);

        void SetDescription(int paymentId, string description);
        void SetCategory(int paymentId, int categoryId);
        void SetInvisible(int paymentId);

        IEnumerable<Payment> GetPayments(int pageNumber = 0, int pageSize = 100);
        IEnumerable<Payment> GetForPeriod(DateRange period);
    }
}
