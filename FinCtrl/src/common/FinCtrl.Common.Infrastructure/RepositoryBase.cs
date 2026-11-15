using Dapper;

namespace FinCtrl.Common.Infrastructure
{
    /// <summary>
    /// База репозиториев: async-хелперы Dapper поверх соединения из <see cref="IDbConnectionFactory"/>.
    /// Все запросы идут в текущей транзакции фабрики, если она открыта, поэтому соединение наружу не отдаётся.
    /// </summary>
    public abstract class RepositoryBase
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public RepositoryBase(IDbConnectionFactory dbConnectionFactory)
        {
            _connectionFactory = dbConnectionFactory;
        }

        /// <summary>Выполняет команду (для набора параметров — по разу на каждый элемент).</summary>
        /// <returns>Число затронутых строк.</returns>
        protected async Task<int> Execute(string sql, object? param = null)
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return await conn.ExecuteAsync(sql, param, _connectionFactory.CurrentTransaction);
        }

        /// <summary>Выполняет запрос и возвращает первую колонку первой строки (например, id из <c>RETURNING</c>).</summary>
        protected async Task<T?> ExecuteScalar<T>(string sql, object? param = null)
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return await conn.ExecuteScalarAsync<T>(sql, param, _connectionFactory.CurrentTransaction);
        }

        /// <summary>Выполняет запрос и маппит строки на <typeparamref name="T"/> по именам колонок.</summary>
        protected async Task<IEnumerable<T>> Query<T>(string sql, object? param = null)
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return await conn.QueryAsync<T>(sql, param, _connectionFactory.CurrentTransaction);
        }

        /// <summary>Первая строка запроса или <c>default</c>, если строк нет.</summary>
        protected async Task<T?> FirstOrDefault<T>(string sql, object? param = null)
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return await conn.QueryFirstOrDefaultAsync<T>(sql, param, _connectionFactory.CurrentTransaction);
        }

        /// <summary>
        /// Начинает транзакцию для многошаговой записи внутри репозитория.
        /// Если сервис уже открыл транзакцию, запись войдёт в неё (вложенная транзакция ничего не фиксирует сама).
        /// </summary>
        protected Task<ITransaction> BeginTransaction() => _connectionFactory.BeginTransaction();
    }
}
