using FinCtrl.Common.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Infrastructure
{
    internal class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private SqlConnection _connection;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlConnection GetConnection()
        {
            if(_connection is not null)
            {
                return _connection;
            }

            var connStr = _configuration.GetConnectionString("DefaultDatabase");

            _connection = new SqlConnection(connStr);
            _connection.Open();
            return _connection;
        }

        ~DbConnectionFactory()
        {
            if (_connection is null)
            {
                return;
            }

            if(_connection.State != ConnectionState.Closed)
            {
                _connection.Close();   
            }
            _connection.Dispose();
        }
    }
}
