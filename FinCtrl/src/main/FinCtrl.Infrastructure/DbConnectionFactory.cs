using FinCtrl.Common;
using FinCtrl.Common.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FinCtrl.Infrastructure
{
    /// <summary>
    /// Одно соединение на scope (запрос) и его транзакция. Реализует и <see cref="IUnitOfWork"/>:
    /// сервис открывает транзакцию, репозитории того же scope автоматически работают в ней.
    /// </summary>
    internal class DbConnectionFactory : IDbConnectionFactory, IAsyncDisposable, IDisposable
    {
        private readonly IConfiguration _configuration;
        private SqlConnection? _connection;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlTransaction? CurrentTransaction { get; private set; }

        public async Task<SqlConnection> GetConnectionAsync()
        {
            if (_connection is not null)
            {
                return _connection;
            }

            var connStr = _configuration.GetConnectionString("DefaultDatabase");

            _connection = new SqlConnection(connStr);
            await _connection.OpenAsync();
            return _connection;
        }

        public async Task<ITransaction> BeginTransaction(CancellationToken cancellationToken = default)
        {
            if (CurrentTransaction is not null)
            {
                return NestedTransaction.Instance;
            }

            var connection = await GetConnectionAsync();
            CurrentTransaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            return new RootTransaction(this, CurrentTransaction);
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            _connection = null;
            CurrentTransaction = null;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _connection = null;
            CurrentTransaction = null;
        }

        /// <summary>Транзакция верхнего уровня: фиксирует или (при освобождении без фиксации) откатывает изменения.</summary>
        private sealed class RootTransaction(DbConnectionFactory owner, SqlTransaction transaction) : ITransaction
        {
            public Task Commit(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

            // SqlTransaction при освобождении без Commit сам откатывает изменения и не бросает,
            // если сервер уже откатил транзакцию из-за ошибки, — исходное исключение не маскируется.
            public async ValueTask DisposeAsync()
            {
                await transaction.DisposeAsync();
                owner.CurrentTransaction = null;
            }
        }

        /// <summary>Транзакция внутри уже открытой: фиксирует и откатывает внешняя.</summary>
        private sealed class NestedTransaction : ITransaction
        {
            public static readonly NestedTransaction Instance = new();

            public Task Commit(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
