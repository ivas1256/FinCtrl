using Dapper;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Infrastructure.PaymentSources
{
    internal class PaymentSourceRepository : RepositoryBase, IPaymentSourceRepository
    {
        public PaymentSourceRepository(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
        {
        }

        public List<PaymentSource> GetAll()
        {
            var sql = @"SELECT
                          ps.PaymentSourceId
                         ,ps.PaymentSourceName
                         ,ps.CategoryId
                         ,ps.CreatedAt
                         ,ps.LastUpdatedAt
                         ,ps.Description
                      FROM PaymentSources ps;";

            var conn = _connectionFactory.GetConnection();

            return conn.Query(sql)
                .Select(x => new PaymentSource(x.PaymentSourceId, x.PaymentSourceName, x.CategoryId
                    , x.Description, x.CreatedAt, x.LastUpdatedAt))
                .ToList();
        }

        public PaymentSource? GetById(int paymentSourceId)
        {
            var sql = @"SELECT
                          ps.PaymentSourceId
                         ,ps.PaymentSourceName
                         ,ps.CategoryId
                         ,ps.CreatedAt
                         ,ps.LastUpdatedAt
                         ,ps.Description
                      FROM PaymentSources ps
                      WHERE PaymentSourceId = @paymentSourceId;";

            var conn = _connectionFactory.GetConnection();

            return conn.Query(sql, new { paymentSourceId })
                .Select(x => new PaymentSource(x.PaymentSourceId, x.PaymentSourceName, x.CategoryId,
                    x.Description, x.CreatedAt, x.LastUpdatedAt))
                .SingleOrDefault();
        }

        public PaymentSource? GetByName(string paymentSourceName)
        {
            var sql = @"SELECT
                     ps.PaymentSourceId
                    ,ps.PaymentSourceName
                    ,ps.CategoryId
                    ,ps.CreatedAt
                    ,ps.LastUpdatedAt
                    ,ps.Description
                  FROM PaymentSources ps
                  WHERE ps.PaymentSourceName = @paymentSourceName";

            var conn = _connectionFactory.GetConnection();

            return conn.QueryFirstOrDefault<PaymentSource>(sql, new { paymentSourceName });
        }

        public void Delete(params PaymentSource[] paymentSources)
        {
            foreach (var paymentSource in paymentSources)
            {
                base.Delete(paymentSource);
            }
        }

        public bool Exists(string paymentSourceName)
        {
            return GetByName(paymentSourceName) is not null;
        }

        public void Update(params PaymentSource[] paymentSources)
        {
            var sql = @"UPDATE PaymentSources
                        SET PaymentSourceName = @PaymentSourceName
                           ,CategoryId = @CategoryId
                           ,LastUpdatedAt = @LastUpdatedAt
                           ,Description = @Description
                        WHERE PaymentSourceId = @PaymentSourceId;";

            foreach (var paymentSource in paymentSources)
            {
                Execute(sql, new
                {
                    paymentSource.PaymentSourceId,
                    paymentSource.PaymentSourceName,
                    paymentSource.PaymentSourceCategory?.CategoryId,
                    paymentSource.LastUpdatedAt,
                    paymentSource.Description
                });
            }
        }

        public int Create(string paymentSourceName)
        {
            var sql = @"INSERT INTO dbo.PaymentSources (PaymentSourceName)
                OUTPUT INSERTED.Id    
                VALUES (@PaymentSourceName)";

            var conn = _connectionFactory.GetConnection();

            return conn.QuerySingle<int>(sql, new { paymentSourceName });
        }
    }
}
