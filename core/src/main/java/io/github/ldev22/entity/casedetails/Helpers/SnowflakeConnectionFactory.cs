using Ade.Club51.Case.Details.Config;
using Ade.Club51.Case.Details.Interface;
using Amazon.Lambda.Core;
using Snowflake.Data.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.Details.Helpers
{
    public class SnowflakeConnectionFactory : ISnowflakeConnectionFactory
    {
        private readonly DatabaseConfig _databaseConfig;
        public SnowflakeConnectionFactory(DatabaseConfig databaseConfig)
        {

            _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));

        }

        public SnowflakeDbConnection CreateConnection()
        {

            try
            {
                LambdaLogger.Log("INFO: Creating Snowflake database connection.");

                var connectionString = GetConnectionString();
                LambdaLogger.Log("INFO: Connection string generated successfully. Masking sensitive details for logs.");

                var maskedConnectionString = $"account={_databaseConfig.DbEdrAccount};user={_databaseConfig.DbEdrUser};password=******;" +
                                             $"scheme=https;host={_databaseConfig.DbEdrServer};port={_databaseConfig.DbEdrPort};" +
                                             $"db={_databaseConfig.DbEdrDb};schema={_databaseConfig.DbEdrSchemaClun51};insecureMode=true";

                LambdaLogger.Log($"INFO: Using connection string: {maskedConnectionString}");

                var connection = new SnowflakeDbConnection(connectionString);
                LambdaLogger.Log("INFO: SnowflakeDbConnection instance created successfully.");

                return connection;
            }
            catch (Exception ex)
            {
                LambdaLogger.Log($"ERROR: Failed to create Snowflake database connection - {ex.Message} | StackTrace: {ex.StackTrace}");
                throw;
            }

        }

        private string GetConnectionString()
        {
            LambdaLogger.Log("INFO: Generating Snowflake connection string.");

            return $"account={_databaseConfig.DbEdrAccount};user={_databaseConfig.DbEdrUser};password={_databaseConfig.DbEdrPassword};" +
                   $"scheme=https;host={_databaseConfig.DbEdrServer};port={_databaseConfig.DbEdrPort};" +
                   $"db={_databaseConfig.DbEdrDb};schema={_databaseConfig.DbEdrSchemaClun51};insecureMode=true";
        }
    }
}
