using Ade.Club51.Case.List.Abstractions;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public SnowflakeConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IDbConnection CreateConnection()
        {
            IDbConnection conn = new SnowflakeDbConnection
            {
                ConnectionString = _configuration["SnowflakeConnectionString"]
            };
            return conn;
        }
    }
}
