using Dapper;
using Dommel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Common.Infrastructure
{
    public abstract class RepositoryBase
    {
        protected readonly IDbConnectionFactory _connectionFactory;

        public RepositoryBase(IDbConnectionFactory dbConnectionFactory)
        {
            _connectionFactory = dbConnectionFactory;
        }

        protected int Execute(string sql, object? param = null)
        {
            var conn = _connectionFactory.GetConnection();

            return conn.Execute(sql, param);
        }

        protected IEnumerable<T> Query<T>(string sql, object? param = null) where T : class
        {
            var conn = _connectionFactory.GetConnection();

            return conn.Query<T>(sql, param);
        }

        protected T? FirstOrDefault<T>(string sql, object? param = null) where T : class
        {
            var conn = _connectionFactory.GetConnection();

            return conn.QueryFirstOrDefault<T>(sql, param);
        }

        protected void Update<T>(params T[] entities) where T : class
        {
            var conn = _connectionFactory.GetConnection();

            foreach (var entity in entities)
            {
                conn.Update(entity);
            }
        }

        protected void Delete<T>(params T[] entities) where T : class
        {
            var conn = _connectionFactory.GetConnection();

            foreach (var entity in entities)
            {
                conn.Delete(entity);
            }
        }
    }
}
