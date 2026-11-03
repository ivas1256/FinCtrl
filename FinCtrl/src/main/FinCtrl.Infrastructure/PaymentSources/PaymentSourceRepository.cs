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
                 ,ps.Classification
                 ,ps.IsAiClassified
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
            return (await Query<PaymentSourceRow>(SelectPaymentSource))
                .Select(x => x.MapToPaymentSource())
                .ToList();
        }

        public async Task<PaymentSource?> GetById(int paymentSourceId)
        {
            var sql = SelectPaymentSource + " WHERE ps.PaymentSourceId = @paymentSourceId;";

            return (await Query<PaymentSourceRow>(sql, new { paymentSourceId }))
                .Select(x => x.MapToPaymentSource())
                .SingleOrDefault();
        }

        public async Task<PaymentSource?> GetByName(string paymentSourceName)
        {
            var sql = SelectPaymentSource + " WHERE ps.PaymentSourceName = @paymentSourceName;";

            return (await Query<PaymentSourceRow>(sql, new { paymentSourceName }))
                .Select(x => x.MapToPaymentSource())
                .FirstOrDefault();
        }

        public async Task<List<PaymentSource>> GetLatestUnclassified(int count)
        {
            var sql = $@"SELECT TOP (@count) *
                         FROM ({SelectPaymentSource} WHERE ps.Classification IS NULL) x
                         ORDER BY x.CreatedAt DESC, x.PaymentSourceId DESC;";

            return (await Query<PaymentSourceRow>(sql, new { count }))
                .Select(x => x.MapToPaymentSource())
                .ToList();
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
                           ,Classification = @Classification
                           ,IsAiClassified = @IsAiClassified
                        WHERE PaymentSourceId = @PaymentSourceId;";

            foreach (var paymentSource in paymentSources)
            {
                await Execute(sql, new
                {
                    paymentSource.PaymentSourceId,
                    paymentSource.PaymentSourceName,
                    paymentSource.PaymentSourceCategory?.CategoryId,
                    paymentSource.LastUpdatedAt,
                    paymentSource.Description,
                    paymentSource.Classification,
                    paymentSource.IsAiClassified
                });
            }
        }

        public async Task UpdateClassifications(IReadOnlyList<PaymentSource> paymentSources)
        {
            const string sql = @"UPDATE PaymentSources
                        SET Classification = @Classification
                           ,IsAiClassified = @IsAiClassified
                        WHERE PaymentSourceId = @PaymentSourceId;";

            // Dapper выполняет команду для каждого элемента коллекции.
            await Execute(sql, paymentSources.Select(x => new { x.PaymentSourceId, x.Classification, x.IsAiClassified }).ToList());
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

            var countSql = $"SELECT COUNT(*) FROM PaymentSources ps {where}";
            var totalCount = await ExecuteScalar<int>(countSql, queryParams);

            var dataSql = new StringBuilder(SelectPaymentSource);
            dataSql.Append(where);
            dataSql.Append(" ORDER BY ps.PaymentSourceId DESC");
            if (pageSize.HasValue)
                dataSql.Append(" OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");

            var items = (await Query<PaymentSourceRow>(dataSql.ToString(), queryParams))
                .Select(x => x.MapToPaymentSource())
                .ToList();

            return (items, totalCount);
        }

        public async Task<int> Create(PaymentSource paymentSource)
        {
            var sql = @"INSERT INTO dbo.PaymentSources (PaymentSourceName, CategoryId, CreatedAt, LastUpdatedAt, Description, Classification, IsAiClassified)
                        OUTPUT INSERTED.PaymentSourceId
                        VALUES (@PaymentSourceName, @CategoryId, @CreatedAt, @LastUpdatedAt, @Description, @Classification, @IsAiClassified);";

            return await ExecuteScalar<int>(sql, new
            {
                paymentSource.PaymentSourceName,
                paymentSource.PaymentSourceCategory?.CategoryId,
                paymentSource.Description,
                paymentSource.CreatedAt,
                paymentSource.LastUpdatedAt,
                paymentSource.Classification,
                paymentSource.IsAiClassified
            });
        }
    }
}
