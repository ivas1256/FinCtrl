using Dapper;
using FinCtrl.Application.PaymentSources;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;
using System.Text;

namespace FinCtrl.Infrastructure.PaymentSources
{
    internal class PaymentSourceRepository : RepositoryBase, IPaymentSourceRepository
    {
        public PaymentSourceRepository(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
        {
        }

        // Общий SELECT источника: категория подтягивается JOIN'ом, а не отдельным запросом на каждый источник.
        private const string SelectPaymentSource = @"SELECT
                  ps.PaymentSourceId
                 ,ps.PaymentSourceName
                 ,ps.Description
                 ,ps.CreatedAt
                 ,ps.LastUpdatedAt
                 ,c.CategoryId Category_CategoryId
                 ,c.CategoryName Category_CategoryName
                 ,c.ParentCategoryCategoryId Category_ParentCategoryId
                 ,c.IsOnlyIncome Category_IsOnlyIncome
                 ,c.OrderIndex Category_OrderIndex
                 ,c.Color Category_Color
                 ,c.CreatedAt Category_CreatedAt
                 ,c.LastUpdatedAt Category_LastUpdatedAt
              FROM PaymentSources ps
              LEFT JOIN Categories c ON c.CategoryId = ps.CategoryId";

        public async Task<List<PaymentSource>> GetAll()
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return (await conn.QueryAsync<PaymentSourceRow>(SelectPaymentSource))
                .Select(x => x.MapToPaymentSource())
                .ToList();
        }

        public async Task<PaymentSource?> GetById(int paymentSourceId)
        {
            var sql = SelectPaymentSource + " WHERE ps.PaymentSourceId = @paymentSourceId;";

            var conn = await _connectionFactory.GetConnectionAsync();

            return (await conn.QueryAsync<PaymentSourceRow>(sql, new { paymentSourceId }))
                .Select(x => x.MapToPaymentSource())
                .SingleOrDefault();
        }

        public async Task<PaymentSource?> GetByName(string paymentSourceName)
        {
            var sql = SelectPaymentSource + " WHERE ps.PaymentSourceName = @paymentSourceName;";

            var conn = await _connectionFactory.GetConnectionAsync();

            return (await conn.QueryAsync<PaymentSourceRow>(sql, new { paymentSourceName }))
                .Select(x => x.MapToPaymentSource())
                .FirstOrDefault();
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

            var dataSql = new StringBuilder(SelectPaymentSource);
            dataSql.Append(where);
            dataSql.Append(" ORDER BY ps.PaymentSourceId DESC");
            if (pageSize.HasValue)
                dataSql.Append(" OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");

            var items = (await conn.QueryAsync<PaymentSourceRow>(dataSql.ToString(), queryParams))
                .Select(x => x.MapToPaymentSource())
                .ToList();

            return (items, totalCount);
        }

        public async Task<int> Create(PaymentSource paymentSource)
        {
            var sql = @"INSERT INTO dbo.PaymentSources (PaymentSourceName, CategoryId, CreatedAt, LastUpdatedAt, Description)
                        OUTPUT INSERTED.PaymentSourceId
                        VALUES (@PaymentSourceName, @CategoryId, @CreatedAt, @LastUpdatedAt, @Description);";

            return await ExecuteScalar<int>(sql, new
            {
                paymentSource.PaymentSourceName,
                paymentSource.PaymentSourceCategory?.CategoryId,
                paymentSource.Description,
                paymentSource.CreatedAt,
                paymentSource.LastUpdatedAt
            });
        }
    }
}
