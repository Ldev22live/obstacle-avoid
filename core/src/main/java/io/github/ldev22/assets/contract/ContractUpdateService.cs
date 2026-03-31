




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

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                // Transform input to SP expected format like CaseService does with EventModel
                var eventPayload = CreateEventPayload(input);
                var json = System.Text.Json.JsonSerializer.Serialize(eventPayload, options);
                var escaped = "'" + json.Replace("'", "''") + "'";

                var db = _dbConfig.DbEdrDb;
                var schema = _dbConfig.DbEdrSchemaClun51;
                var procName = Environment.GetEnvironmentVariable("CONTRACTDETAIL_PROC") ?? "CREATEUPDATE_CONTRACTDETAILS";

                // Create payload for DAECD parameter - expects detail.contractDetails array
                var contractPayload = CreateContractPayload(input);
                var json = System.Text.Json.JsonSerializer.Serialize(contractPayload, options);
                var escapedJson = "'" + json.Replace("'", "''") + "'";
                var caseIdParam = "'" + (input.CaseId ?? "0").Replace("'", "''") + "'";

                using var command = connection.CreateCommand();
                command.CommandText = $"CALL {db}.{schema}.{procName}({caseIdParam}, {escapedJson})";
                command.CommandType = CommandType.Text;

                LambdaLogger.Log($"INFO: Executing: {command.CommandText}");

                // Sync ExecuteReader like CaseService
                using var reader = command.ExecuteReader();
                string? procResult = null;

                while (reader.Read())
                {
                    procResult = reader[0]?.ToString();
                    LambdaLogger.Log($"INFO: SP returned: {procResult}");
                }

                if (string.IsNullOrWhiteSpace(procResult))
                {
                    throw new InvalidOperationException("No ID returned from stored procedure.");
                }

                return new ResponseData
                {
                    data = new Data { newContractDetailId = procResult },
                    IsValid = true,
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                var errorEx = ex.InnerException ?? ex;
                LambdaLogger.Log($"ERROR: UpdateContract failed: {errorEx.Message}");
                throw;
            }
        }

        private object CreateContractPayload(RequestData input)
        {
            return new
            {
                caseId = input.CaseId ?? "0",
                detail = new
                {
                    contractDetails = new[]
                    {
                        new
                        {
                            contractDetailId = input.ContactDetail?.ContractDetailId,
                            contractNumber = input.ContactDetail?.ContractNumber,
                            productName = input.ContactDetail?.ProductName,
                            investType = input.ContactDetail?.InvestType,
                            investAmount = input.ContactDetail?.InvestAmount,
                            payFrequency = (string?)null,
                            payMethod = input.ContactDetail?.PayMethod,
                            commAllowance = input.ContactDetail?.CommAllowance,
                            fpFee = input.ContactDetail?.FpFee,
                            negCommAllowance = input.ContactDetail?.NegCommAllowance,
                            negCommPercentage = input.ContactDetail?.NegCommPercentage,
                            createdBy = input.ContactDetail?.CreatedBy,
                            createdOn = input.ContactDetail?.CreatedOn,
                            modifiedBy = input.ContactDetail?.ModifiedBy,
                            modifiedOn = input.ContactDetail?.ModifiedOn
                        }
                    }
                }
            };
        }

        Task<ResponseData> IContractUpdateService.UpdateContract(RequestData input) => UpdateContract(input);
    }
}
