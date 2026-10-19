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

        public async Task<List<PaymentSource>> GetAll()
        {
            var sql = @"SELECT
                          ps.PaymentSourceId
                         ,ps.PaymentSourceName
                         ,ps.CategoryId
                         ,ps.CreatedAt
                         ,ps.LastUpdatedAt
                         ,ps.Description
                      FROM PaymentSources ps;";

            var conn = await _connectionFactory.GetConnectionAsync();

            return conn.Query(sql)
                .Select(x => new PaymentSource(x.PaymentSourceId, x.PaymentSourceName, x.CategoryId
                    , x.Description, x.CreatedAt, x.LastUpdatedAt))
                .ToList();
        }

        public async Task<PaymentSource?> GetById(int paymentSourceId)
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

            var conn = await _connectionFactory.GetConnectionAsync();

            return conn.Query(sql, new { paymentSourceId })
                .Select(x => new PaymentSource(x.PaymentSourceId, x.PaymentSourceName, x.CategoryId,
                    x.Description, x.CreatedAt, x.LastUpdatedAt))
                .SingleOrDefault();
        }

        public async Task<PaymentSource?> GetByName(string paymentSourceName)
        {
            var sql = @"SELECT
                     ps.PaymentSourceId
                    ,ps.PaymentSourceName
                    ,ps.CategoryId
                    ,ps.Description              
                    ,ps.CreatedAt
                    ,ps.LastUpdatedAt
                  FROM PaymentSources ps
                  WHERE ps.PaymentSourceName = @paymentSourceName";

            var conn = await _connectionFactory.GetConnectionAsync();

            return conn.Query(sql, new { paymentSourceName })
                .Select(x => new PaymentSource(x.PaymentSourceId, x.PaymentSourceName, x.CategoryId,
                    x.Description, x.CreatedAt, x.LastUpdatedAt))
                .FirstOrDefault();
        }

        public async Task Delete(params PaymentSource[] paymentSources)
        {
            var sql = @"DELETE FROM dbo.PaymentSources WHERE PaymentSourceId = @PaymentSourceId;";

            foreach (var paymentSource in paymentSources)
            {
                await Execute(sql, new { paymentSource.PaymentSourceId });
            }
        }

        public async Task<bool> Exists(string paymentSourceName)
        {
            return await GetByName(paymentSourceName) is not null;
        }

        public async Task Update(params PaymentSource[] paymentSources)
        {
            var sql = @"UPDATE PaymentSources
                        SET PaymentSourceName = @PaymentSourceName
                           ,CategoryId = @CategoryId
                           ,LastUpdatedAt = @LastUpdatedAt
                           ,Description = @Description
                        WHERE PaymentSourceId = @PaymentSourceId;";

            foreach (var paymentSource in paymentSources)
            {
                await Execute(sql, new
                {
                    paymentSource.PaymentSourceId,
                    paymentSource.PaymentSourceName,
                    paymentSource.PaymentSourceCategory?.CategoryId,
                    paymentSource.LastUpdatedAt,
                    paymentSource.Description
                });
            }
        }

        public async Task<(List<PaymentSource> Items, int TotalCount)> GetListPaged(
            string? search,
            bool noCategoryOnly,
            int page,
            int? pageSize)
        {
            var searchParam = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";

            var where = new StringBuilder(" WHERE 1=1");
            if (searchParam != null)
                where.Append(" AND ps.PaymentSourceName LIKE @Search");
            if (noCategoryOnly)
                where.Append(" AND ps.CategoryId IS NULL");

            var queryParams = new
            {
                Search = searchParam,
                Skip = (page - 1) * (pageSize ?? 0),
                Take = pageSize
            };

            var conn = await _connectionFactory.GetConnectionAsync();

            var countSql = $"SELECT COUNT(*) FROM PaymentSources ps {where}";
            var totalCount = await conn.ExecuteScalarAsync<int>(countSql, queryParams);

            var dataSql = new StringBuilder(@"SELECT
                ps.PaymentSourceId, ps.PaymentSourceName, ps.CategoryId,
                ps.CreatedAt, ps.LastUpdatedAt, ps.Description
            FROM PaymentSources ps");
            dataSql.Append(where);
            dataSql.Append(" ORDER BY ps.PaymentSourceId DESC");
            if (pageSize.HasValue)
                dataSql.Append(" OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");

            var items = (await conn.QueryAsync(dataSql.ToString(), queryParams))
                .Select(x => new PaymentSource(
                    (int)x.PaymentSourceId,
                    (string)x.PaymentSourceName,
                    (int?)x.CategoryId,
                    (string?)x.Description,
                    (DateTime)x.CreatedAt,
                    (DateTime?)x.LastUpdatedAt))
                .ToList();

            return (items, totalCount);
        }

        public async Task<int> Create(PaymentSource paymentSource)
        {
            var sql = @"DECLARE @generated_keys table([Id] int)

INSERT INTO dbo.PaymentSources (PaymentSourceName
                  , CategoryId
                  , CreatedAt
                  , LastUpdatedAt
                  , Description)
  OUTPUT INSERTED.PaymentSourceId INTO @generated_keys
                  VALUES (@PaymentSourceName, @CategoryId, @CreatedAt, @LastUpdatedAt, @Description);

SELECT * FROM @generated_keys";

            var conn = await _connectionFactory.GetConnectionAsync();

            var id = conn.ExecuteScalar<int>(sql, new
            {
                paymentSource.PaymentSourceName,
                paymentSource.PaymentSourceCategory?.CategoryId,
                paymentSource.Description,
                paymentSource.CreatedAt,
                paymentSource.LastUpdatedAt
            });

            //var id = conn.ExecuteScalar<int>("SELECT SCOPE_IDENTITY();");
            return id;
        }
    }
}
