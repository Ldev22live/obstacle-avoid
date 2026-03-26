using Ade.Club51.Case.Details.Config;
using Ade.Club51.Case.Details.Interface;
using Ade.Club51.Case.Details.Models;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using System.Data;
using Ade.Club51.Case.Details.Helpers;

namespace Ade.Club51.Case.Details.Helpers
{
    public class SearchDataHelper
    {
        private readonly ISnowflakeConnectionFactory _connectionFactory;
        private readonly DatabaseConfig _dbConfig;

        public SearchDataHelper(ISnowflakeConnectionFactory connectionFactory, DatabaseConfig dbConfig)
        {
            _connectionFactory = connectionFactory;
            _dbConfig = dbConfig;
        }
        public async Task<ResponseData> GetSearchData(RequestData requestData)
        {
            var response = new ResponseData();

            try
            {
                LambdaLogger.Log($"INFO: Starting GetSearchData for CaseId = {requestData.CaseId}");

                using (var connection = _connectionFactory.CreateConnection())
                {
                    LambdaLogger.Log("INFO: Creating database connection...");

                    await connection.OpenAsync();
                    LambdaLogger.Log("INFO: Database connection opened successfully.");

                    var caseDetailQuery = CaseQueries.GetCaseDetailQuery(_dbConfig.DbEdrDb, _dbConfig.DbEdrSchemaClun51, _dbConfig.DbEdrTableCase);
                    var financialInfoQuery = CaseQueries.GetFinancialInfoQuery(_dbConfig.DbEdrDb, _dbConfig.DbEdrSchemaClun51, _dbConfig.DbEdrTableFinancialInformation);
                    var contractDetailQuery = CaseQueries.GetContractDetailQuery(_dbConfig.DbEdrDb, _dbConfig.DbEdrSchemaClun51, _dbConfig.DbEdrTableContractDetail);
                    var exceptionQuery = CaseQueries.GetExceptionQuery(_dbConfig.DbEdrDb, _dbConfig.DbEdrSchemaClun51, _dbConfig.DbEdrTableExceptionExclusionLog);
                    var notesQuery = CaseQueries.GetNotesQuery(_dbConfig.DbEdrDb, _dbConfig.DbEdrSchemaClun51, _dbConfig.DbEdrTableNotes);

                    // Execute and log queries
                    LambdaLogger.Log("INFO: Executing query for CaseDetail...");
                    LambdaLogger.Log($"DEBUG: Query: {caseDetailQuery} | Parameters: {{ CaseId: {requestData.CaseId} }}");
                    //var caseDetail = await connection.QueryFirstOrDefaultAsync<CaseDetail>(caseDetailQuery, new { CaseId = requestData.CaseId });
                    var caseDetails = await connection.QueryAsync<CaseDetail, OrgInfoDetail, CaseDetail>(
                                                    caseDetailQuery,
                                                    (caseDetail, orgInfo) =>
                                                    {
                                                        caseDetail.OrgInfo = new List<OrgInfoDetail> { orgInfo };
                                                        return caseDetail;
                                                    },
                                                    new { CaseId = requestData.CaseId },
                                                    splitOn: "Area"
                                                );
                    var caseDetail = caseDetails.FirstOrDefault();

                    LambdaLogger.Log($"INFO: CaseDetail query completed. Found = {(caseDetail != null)}");
                    LambdaLogger.Log($"INFO: Retrieved {JsonConvert.SerializeObject(caseDetail)}");




                    LambdaLogger.Log("INFO: Executing query for FinancialInfo...");
                    LambdaLogger.Log($"DEBUG: Query: {financialInfoQuery} | Parameters: {{ CaseId: {requestData.CaseId} }}");
                    var financialInfoResults = await connection.QueryAsync<FinancialInfo, OrgInfoDetail, FinancialInfo>(
                                                 financialInfoQuery,
                                                 (financialInfo, orgInfo) =>
                                                 {
                                                     financialInfo.OrgInfo = new List<OrgInfoDetail> { orgInfo };
                                                     return financialInfo;
                                                 },
                                                 new { CaseId = requestData.CaseId },
                                                 splitOn: "Area"
                                             );

                    var financialInfos = financialInfoResults.ToList();
                    LambdaLogger.Log($"INFO: Retrieved {financialInfos.Count} FinancialInfo records.");
                    LambdaLogger.Log($"INFO: Retrieved {JsonConvert.SerializeObject(financialInfos)}");

                    LambdaLogger.Log("INFO: Executing query for ContractDetails...");
                    LambdaLogger.Log($"DEBUG: Query: {contractDetailQuery} | Parameters: {{ CaseId: {requestData.CaseId} }}");
                    var contractDetails = (await connection.QueryAsync<ContractDetail>(contractDetailQuery, new { CaseId = requestData.CaseId })).ToList();
                    LambdaLogger.Log($"INFO: Retrieved {contractDetails.Count} ContractDetail records.");
                    LambdaLogger.Log($"INFO: Retrieved {JsonConvert.SerializeObject(contractDetails)}");

                    LambdaLogger.Log("INFO: Executing query for Exceptions...");
                    LambdaLogger.Log($"DEBUG: Query: {exceptionQuery} | Parameters: {{ CaseId: {requestData.CaseId} }}");
                    var exceptions = (await connection.QueryAsync<ExceptionElement>(exceptionQuery, new { CaseId = requestData.CaseId })).ToList();
                    LambdaLogger.Log($"INFO: Retrieved {exceptions.Count} Exception records.");
                    LambdaLogger.Log($"INFO: Retrieved {JsonConvert.SerializeObject(exceptions)}");

                    LambdaLogger.Log("INFO: Executing query for Notes...");
                    LambdaLogger.Log($"DEBUG: Query: {notesQuery} | Parameters: {{ CaseId: {requestData.CaseId} }}");
                    var notes = (await connection.QueryAsync<Note>(notesQuery, new { CaseId = requestData.CaseId })).ToList();
                    LambdaLogger.Log($"INFO: Retrieved {notes.Count} Notes records.");
                    LambdaLogger.Log($"INFO: Retrieved {JsonConvert.SerializeObject(notes)}");

                    LambdaLogger.Log("INFO: Populating response data...");

                    if (caseDetails.Any())
                    {
                        try
                        {
                            using (var command = connection.CreateCommand())
                            {
                                var _dbConfig = new DatabaseConfig();

                                // Build input JSON
                                var requestJson = JsonConvert.SerializeObject(new
                                {
                                    caseId = requestData.CaseId,
                                    userCode = requestData.UserCode,
                                    viewDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd") // Only date part, no time
                                });

                                // Convert to single quotes for SQL
                                var singleQuoteJson = requestJson.Replace("\"", "'");

                                LambdaLogger.Log($"INFO: Converted request JSON to single quotes: {singleQuoteJson}");

                                // Build stored procedure call
                                var procCall = $"CALL {_dbConfig.DbEdrDb}.{_dbConfig.DbEdrSchemaClun51}.{_dbConfig.DbEdrProcViewed}({singleQuoteJson})";
                                LambdaLogger.Log($"INFO: Generated procedure call: {procCall}");


                                // Set up command
                                command.CommandText = procCall;
                                command.CommandType = CommandType.Text;
                                LambdaLogger.Log("INFO: Command text and type set on the command object.");

                                using var reader = command.ExecuteReader();
                                LambdaLogger.Log("INFO: Stored procedure executed. Attempting to read response...");

                                var resultMessage = "";
                                int rowCount = 0;

                                while (reader.Read())
                                {
                                    resultMessage = reader[0]?.ToString();
                                    LambdaLogger.Log($"INFO: Row {++rowCount} read from result set. Message: {resultMessage}");
                                }

                                if (string.IsNullOrEmpty(resultMessage))
                                {
                                    LambdaLogger.Log("WARN: No rows returned by the stored procedure or resultMessage is empty.");
                                }
                                else
                                {
                                    LambdaLogger.Log($"INFO: Final resultMessage after reading: {resultMessage}");
                                }

                                LambdaLogger.Log("INFO: Preparing response object based on resultMessage...");

                                resultMessage = resultMessage?.Trim().Trim('"');
                                LambdaLogger.Log($"INFO: Cleaned result message: {resultMessage}");


                                if (resultMessage.ToString() == "Insertion successful")
                                {
                                    response.Data = new Data
                                    {
                                        CaseDetail = caseDetail ?? new CaseDetail(),
                                        Notes = notes ?? new List<Note>(),
                                        FinancialInfo = financialInfos ?? new List<FinancialInfo>(),
                                        ContractDetails = contractDetails ?? new List<ContractDetail>(),
                                        Exceptions = exceptions ?? new List<ExceptionElement>()
                                    };

                                    response.IsValid = true;
                                    response.Messages.Add(resultMessage ?? "Unknown message from stored procedure.");
                                    response.StatusCode = 200;

                                    LambdaLogger.Log("INFO: Insertion confirmed. Response data successfully populated.");
                                    LambdaLogger.Log($"INFO: Response content: {JsonConvert.SerializeObject(response)}");
                                }
                                else
                                {
                                    response.Data = new Data
                                    {
                                        CaseDetail = caseDetail ?? new CaseDetail(),
                                        Notes = notes ?? new List<Note>(),
                                        FinancialInfo = financialInfos ?? new List<FinancialInfo>(),
                                        ContractDetails = contractDetails ?? new List<ContractDetail>(),
                                        Exceptions = exceptions ?? new List<ExceptionElement>()
                                    };

                                    response.IsValid = true;
                                    response.StatusCode = 200;
                                    response.Errors.Add(resultMessage.ToString() ?? "Unknown error from stored procedure.");

                                    LambdaLogger.Log("WARN: Insertion not confirmed or failed.");
                                    LambdaLogger.Log($"ERROR: Stored procedure returned message: {resultMessage}");
                                    LambdaLogger.Log($"INFO: Response content (with errors): {JsonConvert.SerializeObject(response)}");
                                }

                                LambdaLogger.Log("INFO: Final response returned from method.");
                                return response;

                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to execute procedure: {ex}");
                            LambdaLogger.Log($"ERROR: Failed to execute stored procedure: {ex.Message}");

                            throw new ApplicationException("Failed to mark case as viewed", ex);
                        }
                    }
                    else
                    {
                        response.Data = new Data
                        {
                            CaseDetail = caseDetail ?? new CaseDetail(),
                            Notes = notes ?? new List<Note>(),
                            FinancialInfo = financialInfos ?? new List<FinancialInfo>(),
                            ContractDetails = contractDetails ?? new List<ContractDetail>(),
                            Exceptions = exceptions ?? new List<ExceptionElement>()
                        };

                        response.IsValid = true;
                        response.Messages.Add("No case found in FACT_51CLUB_CASES");
                        response.StatusCode = 200;

                        LambdaLogger.Log("INFO: Insertion confirmed. Response data successfully populated.");
                        LambdaLogger.Log($"INFO: Response content: {JsonConvert.SerializeObject(response)}");

                    }

                }
            }
            catch (Exception ex)
            {
                LambdaLogger.Log($"ERROR: Exception in GetSearchData - {ex.Message}");
                LambdaLogger.Log($"ERROR: StackTrace - {ex.StackTrace}");
                response.Errors.Add(ex.Message);
            }
            finally
            {
                LambdaLogger.Log("INFO: GetSearchData execution completed.");
            }

            return response;
        }
    }
}
