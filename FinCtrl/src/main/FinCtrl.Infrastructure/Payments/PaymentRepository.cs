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

                tr.TransferId,
                CASE WHEN tr.OutPaymentId = p.PaymentId THEN tr.InPaymentId ELSE tr.OutPaymentId END AS TransferPairPaymentId,

                ps.PaymentSourceId AS PaymentSource_PaymentSourceId,
                ps.PaymentSourceName AS PaymentSource_PaymentSourceName,
                ps.Description AS PaymentSource_Description,
                ps.CreatedAt AS PaymentSource_CreatedAt,
                ps.LastUpdatedAt AS PaymentSource_LastUpdatedAt,
                ps.Classification AS PaymentSource_Classification,
                ps.IsAiClassified AS PaymentSource_IsAiClassified,

                a.AccountId AS Account_AccountId,
                a.ExcelAccountName AS Account_ExcelAccountName,
                a.DisplayName AS Account_DisplayName,
                a.IsSavings AS Account_IsSavings,
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
                c_ps.LastUpdatedAt AS PaymentSourceCategory_LastUpdatedAt,

                pa.AccountId AS TransferPairAccount_AccountId,
                pa.ExcelAccountName AS TransferPairAccount_ExcelAccountName,
                pa.DisplayName AS TransferPairAccount_DisplayName,
                pa.IsSavings AS TransferPairAccount_IsSavings,
                pa.CreatedAt AS TransferPairAccount_CreatedAt,
                pa.LastUpdatedAt AS TransferPairAccount_LastUpdatedAt
            FROM Payments p
            LEFT JOIN PaymentSources ps ON p.PaymentSourceId = ps.PaymentSourceId
            LEFT JOIN Accounts a ON p.AccountId = a.AccountId
            LEFT JOIN Categories c ON p.PaymentCategoryCategoryId = c.CategoryId
            LEFT JOIN Categories c_ps ON c_ps.CategoryId = ps.CategoryId
            -- OutPaymentId и InPaymentId уникальны: платёж входит не больше чем в один перевод, строки не размножаются
            LEFT JOIN Transfers tr ON tr.OutPaymentId = p.PaymentId OR tr.InPaymentId = p.PaymentId
            LEFT JOIN Payments pp ON pp.PaymentId = CASE WHEN tr.OutPaymentId = p.PaymentId THEN tr.InPaymentId ELSE tr.OutPaymentId END
            LEFT JOIN Accounts pa ON pa.AccountId = pp.AccountId";

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
            // DELETE + INSERT — в одной транзакции, иначе ошибка на вставке оставит платёж без тегов.
            await using var transaction = await BeginTransaction();

            var sql = @"DELETE FROM dbo.PaymentTags WHERE PaymentId = @PaymentId;";
            await Execute(sql, new { PaymentId = paymentId });

            if (tagIds.Count > 0)
            {
                var insertSql = @"INSERT INTO dbo.PaymentTags (PaymentId, TagId) VALUES (@PaymentId, @TagId);";
                await Execute(insertSql, tagIds.Select(tagId => new { PaymentId = paymentId, TagId = tagId }));
            }

            await transaction.Commit();
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

            // Dapper выполняет команду по разу на пару — без транзакции при ошибке часть тегов осталась бы назначенной.
            await using var transaction = await BeginTransaction();
            await Execute(sql, pairs);
            await transaction.Commit();
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
            // Сумма пишется нормализованной (расход — отрицательная, см. Payment.NormalizeSumSign): иначе расход, сохранённый
            // с положительной суммой, при смене типа на Transfer стал бы поступлением
            var sql = @"UPDATE dbo.Payments
                SET
                  PaymentType = @PaymentType
                 ,PaymentSum = @PaymentSum
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
                    payment.PaymentSum,
                    payment.PaymentSource?.PaymentSourceId,
                    AccountId = payment.Account?.AccountId,
                    PaymentCategoryId = payment.PaymentCategory?.CategoryId,
                    payment.PaymentDescription,
                    payment.LastUpdatedAt,
                    payment.ExcludeFromStats
                });
            }
        }

        public async Task<List<Payment>> GetTransferCandidates(int paymentId, decimal absSum, DateTime from, DateTime to, int? excludeAccountId)
        {
            // tr — связь кандидата из SelectPayment; tr.InPaymentId IS NULL — перевод-списание без второй стороны
            var sql = $@"{SelectPayment}
                WHERE p.IsDeleted = 0
                    AND p.PaymentId <> @paymentId
                    AND ABS(p.PaymentSum) = @absSum
                    AND p.PaymentDate >= @from
                    AND p.PaymentDate < @to
                    AND (@excludeAccountId IS NULL OR p.AccountId IS NULL OR p.AccountId <> @excludeAccountId)
                    AND (tr.TransferId IS NULL OR tr.InPaymentId IS NULL
                         OR tr.OutPaymentId = @paymentId OR tr.InPaymentId = @paymentId)";

            return (await Query<PaymentDto>(sql, new { paymentId, absSum, from, to, excludeAccountId }))
                .Select(x => x.MapToPayment())
                .ToList();
        }

        public async Task<List<Payment>> GetByDateAndSums(IReadOnlyCollection<decimal> paymentSums, DateTime paymentDate)
        {
            var sql = $@"{SelectPayment}
                WHERE p.PaymentSum IN @paymentSums AND p.PaymentDate = @paymentDate AND p.IsDeleted = 0
                ORDER BY p.PaymentId";

            return (await Query<PaymentDto>(sql, new { paymentSums, paymentDate }))
                .Select(x => x.MapToPayment())
                .ToList();
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
            DateTime? from,
            DateTime? to,
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

            var whereClause = new StringBuilder(" WHERE p.IsDeleted = 0");
            if (from.HasValue)
                whereClause.Append(" AND p.PaymentDate >= @From");
            if (to.HasValue)
                whereClause.Append(" AND p.PaymentDate < @To");
            if (sourceId.HasValue)
                whereClause.Append(" AND p.PaymentSourceId = @SourceId");
            if (noCategoryOnly)
                whereClause.Append(" AND p.PaymentCategoryCategoryId IS NULL AND ps.CategoryId IS NULL");
            if (tagIds is { Length: > 0 })
                whereClause.Append(" AND EXISTS (SELECT 1 FROM PaymentTags pt WHERE pt.PaymentId = p.PaymentId AND pt.TagId IN @TagIds)");

            var queryParams = new
            {
                From = from,
                To = to,
                SourceId = sourceId,
                TagIds = tagIds,
                Skip = (page - 1) * (pageSize ?? 0),
                Take = pageSize
            };

            // Быстрый COUNT без сортировки и загрузки данных
            var countSql = $@"SELECT COUNT(*)
                FROM Payments p
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
