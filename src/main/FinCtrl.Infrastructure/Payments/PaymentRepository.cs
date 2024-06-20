using Dapper;
using FinCtrl.Common;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Infrastructure.Payments
{
    internal class PaymentRepository : RepositoryBase, IPaymentRepository
    {
        public PaymentRepository(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
        {
        }

        public Payment? Get(int id)
        {
            var sql = @$"SELECT
                      p.PaymentId
                     ,p.PaymentType
                     ,p.PaymentSum
                     ,p.PaymentDate
                     ,p.PaymentDescription
                     ,p.PaymentSourceId
                     ,p.PaymentCategoryCategoryId PaymentCategoryId
                     ,p.CreatedAt
                     ,p.LastUpdatedAt
                    FROM dbo.Payments p
                    WHERE p.PaymentId = @id";

            var conn = _connectionFactory.GetConnection();

            dynamic x = conn.Query(sql, new { id });
            if (x is null)
                return null;

            return new Payment(
                    x.PaymentId, x.PaymentType, x.PaymentSum, x.PaymentDate, x.PaymentDescription,
                    x.PaymentSourceId, x.PaymentCategoryId, x.CreatedAt, x.LastUpdatedAt);
        }

        public List<Payment> GetAll(Pagination pagination)
        {
            // TODO normal pagination
            var sql = @$"SELECT
                      p.PaymentId
                     ,p.PaymentType
                     ,p.PaymentSum
                     ,p.PaymentDate
                     ,p.PaymentDescription
                     ,p.PaymentSourceId
                     ,p.PaymentCategoryCategoryId PaymentCategoryId
                     ,p.CreatedAt
                     ,p.LastUpdatedAt
                    FROM dbo.Payments p";

            var conn = _connectionFactory.GetConnection();

            return conn.Query(sql)
                .Select(x => new Payment(
                    x.PaymentId, x.PaymentType, x.PaymentSum, x.PaymentDate, x.PaymentDescription,
                    x.PaymentSourceId, x.PaymentCategoryId, x.CreatedAt, x.LastUpdatedAt))
                .OrderByDescending(x => x.PaymentId)
                .Skip(pagination.ToSkipAmount)
                .Take(pagination.PageSize)
                .ToList();
        }

        public void Delete(params Payment[] payments)
        {
            base.Delete(payments);
        }

        public int Create(Payment payment)
        {
            throw new NotImplementedException();
        }

        public void Update(params Payment[] payments)
        {
            var sql = @"UPDATE dbo.Payments 
                SET
                  PaymentType = @PaymentType
                 ,PaymentSourceId = @PaymentSourceId 
                 ,PaymentCategoryCategoryId = @PaymentCategoryId
                 ,PaymentDescription = @PaymentDescription
                 ,LastUpdatedAt = @LastUpdatedAt
                WHERE
                  PaymentId = @PaymentId";

            foreach (var payment in payments)
            {
                Execute(sql, new
                {
                    payment.PaymentId,
                    payment.PaymentType,
                    payment.PaymentSource?.PaymentSourceId,
                    payment.PaymentCategory?.ParentCategoryId,
                    payment.PaymentDescription,
                    payment.LastUpdatedAt
                });
            }
        }

        public bool Exists(decimal paymentSum, DateTime paymentDate)
        {
            var sql = @"SELECT
                  *
                  FROM Payments p
                  WHERE p.PaymentSum = @paymentSum
                  AND p.PaymentDate = @paymentDate";

            return FirstOrDefault<object>(sql, new { paymentSum, paymentDate }) is not null;
        }
    }
}
