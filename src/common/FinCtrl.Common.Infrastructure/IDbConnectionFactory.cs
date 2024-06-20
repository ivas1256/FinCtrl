using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Common.Infrastructure
{
    public interface IDbConnectionFactory
    {
        public SqlConnection GetConnection();
    }
}
