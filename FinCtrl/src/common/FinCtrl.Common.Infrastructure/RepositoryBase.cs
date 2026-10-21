using Dapper;

namespace FinCtrl.Common.Infrastructure
{
    public abstract class RepositoryBase
    {
        protected readonly IDbConnectionFactory _connectionFactory;

        public RepositoryBase(IDbConnectionFactory dbConnectionFactory)
        {
            _connectionFactory = dbConnectionFactory;
        }

        protected async Task<int> Execute(string sql, object? param = null)
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return conn.Execute(sql, param);
        }

        protected async Task<IEnumerable<T>> Query<T>(string sql, object? param = null) where T : class
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return await conn.QueryAsync<T>(sql, param);
        }

        protected async Task<T?> FirstOrDefault<T>(string sql, object? param = null) 
        {
            var conn = await _connectionFactory.GetConnectionAsync();

            return conn.QueryFirstOrDefault<T>(sql, param);
        }
    }
}
