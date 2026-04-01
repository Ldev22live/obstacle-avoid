




using Ade.Club51.Lambda.Contract.Update.Config;
using Ade.Club51.Lambda.Contract.Update.Interface;
using Ade.Club51.Lambda.Contract.Update.Models;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Threading.Tasks;

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
                string? spResult = null;

                while (reader.Read())
                {
                    spResult = reader[0]?.ToString();
                    LambdaLogger.Log($"INFO: SP returned: {spResult}");
                }

                if (string.IsNullOrWhiteSpace(spResult) || spResult.StartsWith("ContractDetail Failed"))
                {
                    throw new InvalidOperationException($"SP failed: {spResult}");
                }

                // Query for the new contract detail ID matching CaseQueries pattern
                string newContractDetailId;
                using (var idCommand = connection.CreateCommand())
                {
                    idCommand.CommandText = $@"
                        SELECT CD.CD_ID
                        FROM {db}.{schema}.FIFTYONECLUB_CONTRACTDETAIL CD
                        WHERE CD.CD_CASE_ID = '{input.CaseId}'
                          AND CD.CD_CONTRACTNUMBER = '{input.ContactDetail?.ContractNumber}'
                          AND (CD.CD_ENDDATE IS NULL OR CD.CD_ENDDATE = '9999-12-31')
                          AND CD.CD_ISDELETED = 0";
                    idCommand.CommandType = CommandType.Text;

                    LambdaLogger.Log($"INFO: Querying for new CD_ID: {idCommand.CommandText}");

                    using var idReader = idCommand.ExecuteReader();
                    if (idReader.Read())
                    {
                        newContractDetailId = idReader[0]?.ToString() ?? throw new InvalidOperationException("CD_ID is null");
                        LambdaLogger.Log($"INFO: New contract detail ID: {newContractDetailId}");
                    }
                    else
                    {
                        throw new InvalidOperationException("Could not find new contract detail record after update.");
                    }
                }

                return new ResponseData
                {
                    data = new Data { newContractDetailId = newContractDetailId },
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
                            payFrequency = input.ContactDetail?.PayFrequency,
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

2026-04-01T18:14:14.421Z	05f1dd01-6f2b-4981-ba0c-2ae254d8d6b1	trce	INFO: Executing: CALL DEV_SOURCE.CLUB51.CREATEUPDATE_CONTRACTDETAILS('2061a13a6237e27f9387fb1d881d7578df72f03f37e40e20bdaf2924cd2195b4', '
{
    "caseId": "2061a13a6237e27f9387fb1d881d7578df72f03f37e40e20bdaf2924cd2195b4",
    "detail": {
        "contractDetails": [
            {
                "contractDetailId": "d85e789c-cc36-4a10-b377-e283aa7955fd",
                "contractNumber": "PaulineCD1",
                "productName": "Conventional - Recurring EXB",
                "investType": "Recurring Premium",
                "investAmount": "75222.00",
                "payFrequency": null,
                "payMethod": "3",
                "commAllowance": "22.00",
                "fpFee": "0.00",
                "negCommAllowance": "22.00",
                "negCommPercentage": "100.00",
                "createdBy": "OMDAEsuper",
                "createdOn": "2026-04-01T20:14:12.273",
                "modifiedBy": null,
                "modifiedOn": "0001-01-01T00:00:00"
            }
        ]
    }
}
')
2026-04-01T18:14:15.935Z	05f1dd01-6f2b-4981-ba0c-2ae254d8d6b1	trce	INFO: SP returned: 1
2026-04-01T18:14:15.935Z	05f1dd01-6f2b-4981-ba0c-2ae254d8d6b1	trce	INFO: Querying for new CD_ID:
                        SELECT CD.CD_ID
                        FROM DEV_SOURCE.CLUB51.FIFTYONECLUB_CONTRACTDETAIL CD
                        WHERE CD.CD_CASE_ID = '2061a13a6237e27f9387fb1d881d7578df72f03f37e40e20bdaf2924cd2195b4'
                          AND CD.CD_CONTRACTNUMBER = 'PaulineCD1'
                          AND (CD.CD_ENDDATE IS NULL OR CD.CD_ENDDATE = '9999-12-31')
                          AND CD.CD_ISDELETED = 0
2026-04-01T18:14:16.260Z	05f1dd01-6f2b-4981-ba0c-2ae254d8d6b1	trce	INFO: New contract detail ID: c767dc4b-7f71-40af-a565-9caf6ebcf3b1
2026-04-01T18:14:16.260Z	05f1dd01-6f2b-4981-ba0c-2ae254d8d6b1	trce	INFO: Response received:
{
    "data": {
        "newContractDetailId": "c767dc4b-7f71-40af-a565-9caf6ebcf3b1"
    },
    "isValid": true,
    "statusCode": 200,
    "messages": [],
    "errors": []
}
