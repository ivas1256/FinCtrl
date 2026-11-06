using FinCtrl.Application.Transfers;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.Transfers
{
    internal class TransferRepository : RepositoryBase, ITransferRepository
    {
        public TransferRepository(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
        {
        }

        public async Task<Transfer?> GetByPaymentId(int paymentId)
        {
            // OutPaymentId и InPaymentId уникальны, поэтому платёж входит не больше чем в один перевод
            var sql = @"SELECT
                          t.TransferId
                         ,t.OutPaymentId
                         ,t.InPaymentId
                         ,t.CreatedAt
                      FROM dbo.Transfers t
                      WHERE t.OutPaymentId = @paymentId OR t.InPaymentId = @paymentId";

            return await FirstOrDefault<Transfer>(sql, new { paymentId });
        }

        public async Task<int> Create(Transfer transfer)
        {
            var sql = @"INSERT INTO dbo.Transfers (OutPaymentId, InPaymentId, CreatedAt)
                        OUTPUT INSERTED.TransferId
                        VALUES (@OutPaymentId, @InPaymentId, @CreatedAt);";

            return await ExecuteScalar<int>(sql, new
            {
                transfer.OutPaymentId,
                transfer.InPaymentId,
                transfer.CreatedAt
            });
        }

        public async Task Update(Transfer transfer)
        {
            var sql = @"UPDATE dbo.Transfers
                        SET OutPaymentId = @OutPaymentId
                           ,InPaymentId = @InPaymentId
                        WHERE TransferId = @TransferId;";

            await Execute(sql, new
            {
                transfer.TransferId,
                transfer.OutPaymentId,
                transfer.InPaymentId
            });
        }

        public async Task Delete(int transferId)
        {
            await Execute("DELETE FROM dbo.Transfers WHERE TransferId = @transferId;", new { transferId });
        }
    }
}
