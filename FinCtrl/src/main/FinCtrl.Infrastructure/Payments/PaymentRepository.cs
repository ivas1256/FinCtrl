using FinCtrl.Application.Payments;
using FinCtrl.Common;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;
using System.Text;

namespace FinCtrl.Infrastructure.Payments
{

    internal class PaymentRepository : RepositoryBase, IPaymentRepository
    {
        public PaymentRepository(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
        {
        }

        // SQL Server принимает не больше 2100 параметров в запросе, а `IN @Ids` разворачивается в параметр на каждый id.
        private const int MaxIdsPerQuery = 2000;

        // Единственный SELECT платежей: алиасы колонок совпадают со свойствами PaymentDto
        // (соответствие проверяет PaymentRepositoryTests). internal — ради этого теста.
        internal const string SelectPayment = @"SELECT
                p.PaymentId,
                p.PaymentType,
                p.PaymentSum,
                p.PaymentDate,
                p.PaymentSourceId,
                p.AccountId,
                p.PaymentCategoryCategoryId,
                p.PaymentDescription,
                p.CreatedAt AS PaymentCreatedAt,
                p.LastUpdatedAt AS PaymentLastUpdatedAt,
                p.ExcludeFromStats,

                ps.PaymentSourceId AS PaymentSource_PaymentSourceId,
                ps.PaymentSourceName AS PaymentSource_PaymentSourceName,
                ps.Description AS PaymentSource_Description,
                ps.CreatedAt AS PaymentSource_CreatedAt,
                ps.LastUpdatedAt AS PaymentSource_LastUpdatedAt,

                a.AccountId AS Account_AccountId,
                a.ExcelAccountName AS Account_ExcelAccountName,
                a.DisplayName AS Account_DisplayName,
                a.CreatedAt AS Account_CreatedAt,
                a.LastUpdatedAt AS Account_LastUpdatedAt,

                c.CategoryId AS Category_CategoryId,
                c.CategoryName AS Category_CategoryName,
                c.ParentCategoryCategoryId AS Category_ParentCategoryId,
                c.IsOnlyIncome AS Category_IsOnlyIncome,
                c.OrderIndex AS Category_OrderIndex,
                c.CreatedAt AS Category_CreatedAt,
                c.LastUpdatedAt AS Category_LastUpdatedAt,

                c_ps.CategoryId AS PaymentSourceCategory_CategoryId,
                c_ps.CategoryName AS PaymentSourceCategory_CategoryName,
                c_ps.ParentCategoryCategoryId AS PaymentSourceCategory_ParentCategoryId,
                c_ps.IsOnlyIncome AS PaymentSourceCategory_IsOnlyIncome,
                c_ps.OrderIndex AS PaymentSourceCategory_OrderIndex,
                c_ps.CreatedAt AS PaymentSourceCategory_CreatedAt,
                c_ps.LastUpdatedAt AS PaymentSourceCategory_LastUpdatedAt
            FROM Payments p WITH (NOLOCK)
            LEFT JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
            LEFT JOIN Accounts a ON p.AccountId = a.AccountId
            LEFT JOIN Categories c ON p.PaymentCategoryCategoryId = c.CategoryId
            LEFT JOIN Categories c_ps ON c_ps.CategoryId = ps.CategoryId";

        public async Task<Payment?> Get(int paymentId)
        {
            var sql = $"{SelectPayment} WHERE p.PaymentId = @PaymentId AND p.IsDeleted = 0";

            var paymentDto = await FirstOrDefault<PaymentDto>(sql, new { PaymentId = paymentId });

            return paymentDto?.MapToPayment();
        }

        public async Task<List<Payment>> GetByDateRange(DateRange dateRange, int[]? tagIds = null)
        {
            var sql = @$"{SelectPayment}
                WHERE p.PaymentDate >= @From
                    AND p.PaymentDate < @To
                    AND p.IsDeleted = 0
                    AND p.ExcludeFromStats = 0";

            var payments = (await Query<PaymentDto>(sql, new { dateRange.From, dateRange.To }))
                .Select(x => x.MapToPayment())
                .ToList();

            await AttachTags(payments);

            if (tagIds is { Length: > 0 })
            {
                payments = payments.Where(p => p.Tags.Any(t => tagIds.Contains(t.TagId))).ToList();
            }

            return payments;
        }

        private async Task<Dictionary<int, List<Tag>>> GetTagsForPayments(IEnumerable<int> paymentIds)
        {
            var sql = @"SELECT pt.PaymentId, t.TagId, t.TagName, t.CreatedAt, t.LastUpdatedAt
                FROM PaymentTags pt
                JOIN Tags t ON t.TagId = pt.TagId
                WHERE pt.PaymentId IN @Ids";

            var rows = new List<PaymentTagRow>();
            foreach (var chunk in paymentIds.Distinct().Chunk(MaxIdsPerQuery))
            {
                rows.AddRange(await Query<PaymentTagRow>(sql, new { Ids = chunk }));
            }

            return rows
                .GroupBy(x => x.PaymentId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.MapToTag()).ToList());
        }

        private async Task AttachTags(IEnumerable<Payment> payments)
        {
            var tagsByPaymentId = await GetTagsForPayments(payments.Select(p => p.PaymentId));

            foreach (var payment in payments)
            {
                payment.Tags = tagsByPaymentId.TryGetValue(payment.PaymentId, out var tags) ? tags : new List<Tag>();
            }
        }

        public async Task SetPaymentTags(int paymentId, IReadOnlyCollection<int> tagIds)
        {
            var sql = @"DELETE FROM dbo.PaymentTags WHERE PaymentId = @PaymentId;";
            await Execute(sql, new { PaymentId = paymentId });

            if (tagIds.Count > 0)
            {
                var insertSql = @"INSERT INTO dbo.PaymentTags (PaymentId, TagId) VALUES (@PaymentId, @TagId);";
                await Execute(insertSql, tagIds.Select(tagId => new { PaymentId = paymentId, TagId = tagId }));
            }
        }

        public async Task AddTagsToPayments(IEnumerable<int> paymentIds, IEnumerable<int> tagIds)
        {
            var ids = paymentIds.Distinct().ToList();
            var tags = tagIds.Distinct().ToList();
            if (ids.Count == 0 || tags.Count == 0)
                return;

            var sql = @"IF NOT EXISTS (SELECT 1 FROM dbo.PaymentTags WHERE PaymentId = @PaymentId AND TagId = @TagId)
                INSERT INTO dbo.PaymentTags (PaymentId, TagId) VALUES (@PaymentId, @TagId);";

            var pairs = ids.SelectMany(paymentId => tags.Select(tagId => new { PaymentId = paymentId, TagId = tagId }));
            await Execute(sql, pairs);
        }

        public async Task<int> Create(Payment payment)
        {
            var sql = @"INSERT INTO dbo.Payments (PaymentType
                  , PaymentSum
                  , PaymentDate
                  , PaymentSourceId
                  , AccountId
                  , PaymentCategoryCategoryId
                  , PaymentDescription
                  , CreatedAt
                  , LastUpdatedAt
                  , ExcludeFromStats)
                  OUTPUT INSERTED.PaymentId
                  VALUES (@PaymentType
                  , @PaymentSum
                  , @PaymentDate
                  , @PaymentSourceId
                  , @AccountId
                  , @PaymentCategoryId
                  , @PaymentDescription
                  , @CreatedAt
                  , @LastUpdatedAt
                  , @ExcludeFromStats);";

            return await ExecuteScalar<int>(sql, new
            {
                payment.PaymentType,
                payment.PaymentSum,
                payment.PaymentDate,
                payment.PaymentSource!.PaymentSourceId,
                AccountId = payment.Account?.AccountId,
                PaymentCategoryId = payment.PaymentCategory?.CategoryId,
                payment.PaymentDescription,
                payment.CreatedAt,
                payment.LastUpdatedAt,
                payment.ExcludeFromStats
            });
        }

        public async Task Update(params Payment[] payments)
        {
            var sql = @"UPDATE dbo.Payments
                SET
                  PaymentType = @PaymentType
                 ,PaymentSourceId = @PaymentSourceId
                 ,AccountId = @AccountId
                 ,PaymentCategoryCategoryId = @PaymentCategoryId
                 ,PaymentDescription = @PaymentDescription
                 ,LastUpdatedAt = @LastUpdatedAt
                 ,ExcludeFromStats = @ExcludeFromStats
                WHERE
                  PaymentId = @PaymentId";

            foreach (var payment in payments)
            {
                await Execute(sql, new
                {
                    payment.PaymentId,
                    payment.PaymentType,
                    payment.PaymentSource?.PaymentSourceId,
                    AccountId = payment.Account?.AccountId,
                    PaymentCategoryId = payment.PaymentCategory?.CategoryId,
                    payment.PaymentDescription,
                    payment.LastUpdatedAt,
                    payment.ExcludeFromStats
                });
            }
        }

        public async Task<Payment?> FindDuplicate(decimal paymentSum, DateTime paymentDate)
        {
            var sql = $"{SelectPayment} WHERE p.PaymentSum = @paymentSum AND p.PaymentDate = @paymentDate AND p.IsDeleted = 0";

            var dto = await FirstOrDefault<PaymentDto>(sql, new { paymentSum, paymentDate });
            return dto?.MapToPayment();
        }

        public async Task<List<Payment>> GetBySource(int PaymentSourceId)
        {
            var sql = @$"{SelectPayment}
                WHERE p.PaymentSourceId = @PaymentSourceId AND p.IsDeleted = 0";

            return (await Query<PaymentDto>(sql, new { PaymentSourceId }))
                .Select(x => x.MapToPayment())
                .ToList();
        }

        public async Task<(List<Payment> Items, int TotalCount)> GetListPaged(
            DateRange dateRange,
            int? sourceId,
            bool noCategoryOnly,
            string sortBy,
            string sortDir,
            int page,
            int? pageSize,
            int[]? tagIds = null)
        {
            var orderCol = sortBy switch
            {
                "source"   => "ps.PaymentSourceName",
                "category" => "COALESCE(c.CategoryName, c_ps.CategoryName)",
                _          => "p.PaymentDate"
            };
            var orderDir = sortDir == "asc" ? "ASC" : "DESC";

            var whereClause = new StringBuilder(
                " WHERE p.IsDeleted = 0 AND p.PaymentDate >= @From AND p.PaymentDate < @To");
            if (sourceId.HasValue)
                whereClause.Append(" AND p.PaymentSourceId = @SourceId");
            if (noCategoryOnly)
                whereClause.Append(" AND p.PaymentCategoryCategoryId IS NULL AND ps.CategoryId IS NULL");
            if (tagIds is { Length: > 0 })
                whereClause.Append(" AND EXISTS (SELECT 1 FROM PaymentTags pt WHERE pt.PaymentId = p.PaymentId AND pt.TagId IN @TagIds)");

            var queryParams = new
            {
                dateRange.From,
                dateRange.To,
                SourceId = sourceId,
                TagIds = tagIds,
                Skip = (page - 1) * (pageSize ?? 0),
                Take = pageSize
            };

            // Быстрый COUNT без сортировки и загрузки данных
            var countSql = $@"SELECT COUNT(*)
                FROM Payments p WITH (NOLOCK)
                LEFT JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
                {whereClause}";
            var totalCount = await ExecuteScalar<int>(countSql, queryParams);

            // Данные: только нужная страница
            var dataSql = new StringBuilder(SelectPayment);
            dataSql.Append(whereClause);
            dataSql.Append($" ORDER BY {orderCol} {orderDir}");
            if (pageSize.HasValue)
                dataSql.Append(" OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");

            var items = (await Query<PaymentDto>(dataSql.ToString(), queryParams))
                .Select(x => x.MapToPayment())
                .ToList();

            await AttachTags(items);

            return (items, totalCount);
        }
    }
}
