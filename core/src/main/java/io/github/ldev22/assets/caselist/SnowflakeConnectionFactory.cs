using Ade.Club51.Case.List.Abstractions;
using Snowflake.Data.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.List.Helpers
{
    public class SnowflakeConnectionFactory : ISnowflakeConnectionFactory
    {
        public IDbConnection CreateConnection()
        {
            IDbConnection conn = new SnowflakeDbConnection
            {
                ConnectionString = Environment.GetEnvironmentVariable("SnowflakeConnectionString")
            };
            return conn;
        }
    }
}
