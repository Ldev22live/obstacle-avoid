




using Ade.Club51.Lambda.Contract.Update.Config;
using Ade.Club51.Lambda.Contract.Update.Interface;
using Ade.Club51.Lambda.Contract.Update.Models;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Apache.Arrow;
using Azure;
using Google.Protobuf.WellKnownTypes;
using Mono.Unix.Native;
using Newtonsoft.Json;
using Snowflake.Data.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Ade.Club51.Lambda.Contract.Update.Services
{
    public class ContractUpdateService : IContractUpdateService
    {
        private readonly ISnowflakeConnectionFactory _connectionFactory;
        private readonly DatabaseConfig _dbConfig;

        public ContractUpdateService(ISnowflakeConnectionFactory connectionFactory, DatabaseConfig dbConfig)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _dbConfig = dbConfig ?? throw new ArgumentNullException(nameof(dbConfig));

            LambdaLogger.Log("INFO: ContractUpdateService initialized successfully.");
        }

        public async Task<ResponseData> UpdateContract(RequestData input)
        {
            LambdaLogger.Log($"INFO: UpdateContract called with input: {JsonConvert.SerializeObject(input)}");

            var response = new ResponseData();

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = System.Text.Json.JsonSerializer.Serialize(input, options);
                var escaped = "'" + json.Replace("'", "''") + "'";

                var db = _dbConfig.DbEdrDb;
                var schema = _dbConfig.DbEdrSchemaClun51;
                var procName = Environment.GetEnvironmentVariable("CONTRACTDETAIL_PROC") ?? "CREATEUPDATE_CASE";

                using var command = connection.CreateCommand();
                command.CommandText = $"CALL {db}.{schema}.{procName}({escaped})";
                command.CommandType = CommandType.Text;

                LambdaLogger.Log($"INFO: Executing stored procedure: {db}.{schema}.{procName}");

                // Use sync ExecuteReader like CaseService (Snowflake driver works better sync)
                using var reader = command.ExecuteReader();
                string? procResult = null;

                while (reader.Read())
                {
                    procResult = reader[0]?.ToString();
                }

                if (string.IsNullOrWhiteSpace(procResult))
                {
                    throw new InvalidOperationException("No ID returned from the stored procedure.");
                }

                // Return the stored procedure result as the new contract detail ID
                response.data = new Data
                {
                    newContractDetailId = procResult,
                };
                response.IsValid = true;
                response.StatusCode = 200;

                LambdaLogger.Log($"INFO: UpdateContract completed. NewContractDetailId: {procResult}");
                return response;
            }
            catch (Exception ex)
            {
                var errorEx = ex.InnerException ?? ex;
                LambdaLogger.Log($"ERROR: UpdateContract failed. Error: {errorEx.Message}");
                throw;
            }
        }

        Task<ResponseData> IContractUpdateService.UpdateContract(RequestData input) => UpdateContract(input);
    }
}
INFO: Using connection string: account = pg70172; user = SRV_DAE_TECH; password = ******; scheme = https; host = pg70172.eu - west - 1.snowflakecomputing.com; port = 443; db = dev_source; schema = CLUB51; insecureMode = true
INFO: SnowflakeDbConnection instance created successfully.
INFO: Executing stored procedure: dev_source.CLUB51.CREATEUPDATE_CASE
INFO: UpdateContract completed. NewContractDetailId: Case Failed: ResultSet is empty or not prepared(call next() first).
INFO: Response received: { "data":{ "newContractDetailId":"Case Failed: ResultSet is empty or not prepared (call next() first)."},"isValid":true,"statusCode":200,"messages":[],"errors":[]}
INFO: FunctionHandler execution completed.
