




using Ade.Club51.Lambda.Contract.Update.Config;
using Ade.Club51.Lambda.Contract.Update.Interface;
using Ade.Club51.Lambda.Contract.Update.Models;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

                // Execute Submission Update SP
                var submissionProcName = Environment.GetEnvironmentVariable("SUBMISSION_PROC") ?? "CREATEUPDATE_SUBMISSIONS";
                var submissionPayload = CreateSubmissionPayload(input);
                var submissionJson = System.Text.Json.JsonSerializer.Serialize(submissionPayload, options);

                // Validate submission payload is not empty
                if (string.IsNullOrWhiteSpace(submissionJson) || submissionJson == "{\"submission\":{}}")
                {
                    LambdaLogger.Log("ERROR: Submission payload is empty - ContactDetail data required");
                    throw new InvalidOperationException("Submission payload is empty. ContactDetail with ContractDetailId is required.");
                }

                var escapedSubmissionJson = "'" + submissionJson.Replace("'", "''") + "'";

                using (var submissionCommand = connection.CreateCommand())
                {
                    submissionCommand.CommandText = $"CALL {db}.{schema}.{submissionProcName}({escapedSubmissionJson})";
                    submissionCommand.CommandType = CommandType.Text;

                    LambdaLogger.Log($"INFO: Executing submission SP: {submissionCommand.CommandText}");

                    using var submissionReader = submissionCommand.ExecuteReader();
                    string? submissionResult = null;
                    while (submissionReader.Read())
                    {
                        submissionResult = submissionReader[0]?.ToString();
                        LambdaLogger.Log($"INFO: Submission SP returned: {submissionResult}");
                    }

                    if (string.IsNullOrWhiteSpace(submissionResult) || submissionResult.StartsWith("Submission Failed"))
                    {
                        throw new InvalidOperationException($"Submission SP failed: {submissionResult}");
                    }
                }

                // Query for the new contract detail ID matching CaseQueries pattern - get most recent
                string newContractDetailId;
                using (var idCommand = connection.CreateCommand())
                {
                    idCommand.CommandText = $@"
                        SELECT CD.CD_ID
                        FROM {db}.{schema}.FIFTYONECLUB_CONTRACTDETAIL CD
                        WHERE CD.CD_CASE_ID = '{input.CaseId}'
                          AND CD.CD_CONTRACTNUMBER = '{input.ContactDetail?.ContractNumber}'
                          AND (CD.CD_ENDDATE IS NULL OR CD.CD_ENDDATE = '9999-12-31')
                          AND CD.CD_ISDELETED = 0
                        ORDER BY CD.CD_MODIFIED DESC
                        LIMIT 1";
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

        private Dictionary<string, object> CreateContractPayload(RequestData input)
        {
            var contractDetail = new Dictionary<string, object?>();

            if (input.ContactDetail != null)
            {
                contractDetail["contractDetailId"] = input.ContactDetail.ContractDetailId;
                contractDetail["contractNumber"] = input.ContactDetail.ContractNumber;
                contractDetail["productName"] = input.ContactDetail.ProductName;
                contractDetail["investType"] = input.ContactDetail.InvestType;
                contractDetail["investAmount"] = input.ContactDetail.InvestAmount;
                contractDetail["payFrequency"] = input.ContactDetail.PayFrequency;
                contractDetail["payMethod"] = input.ContactDetail.PayMethod;
                contractDetail["commAllowance"] = input.ContactDetail.CommAllowance;
                contractDetail["fpFee"] = input.ContactDetail.FpFee;
                contractDetail["negCommAllowance"] = input.ContactDetail.NegCommAllowance;
                contractDetail["negCommPercentage"] = input.ContactDetail.NegCommPercentage;
                contractDetail["createdBy"] = input.ContactDetail.CreatedBy;
                contractDetail["createdOn"] = input.ContactDetail.CreatedOn;
                contractDetail["modifiedBy"] = input.ContactDetail.ModifiedBy;
                contractDetail["modifiedOn"] = input.ContactDetail.ModifiedOn;
            }

            return new Dictionary<string, object>
            {
                ["caseId"] = input.CaseId ?? "0",
                ["detail"] = new Dictionary<string, object>
                {
                    ["contractDetails"] = new[] { contractDetail }
                }
            };
        }

        private Dictionary<string, object> CreateSubmissionPayload(RequestData input)
        {
            var submission = new Dictionary<string, object?>();

            // Map ContactDetail fields to Submission fields where they align
            if (input.ContactDetail != null)
            {
                submission["new51ClubsubmissionId"] = input.ContactDetail.ContractDetailId;
                submission["contractNumber"] = input.ContactDetail.ContractNumber;
                submission["productName"] = input.ContactDetail.ProductName;
                submission["commAllowance"] = input.ContactDetail.CommAllowance;
                submission["fpFee"] = input.ContactDetail.FpFee;
                submission["negCommAllowance"] = input.ContactDetail.NegCommAllowance;
                submission["createdBy"] = input.ContactDetail.CreatedBy;
                submission["modifiedBy"] = input.ContactDetail.ModifiedBy;
            }

            // Use CaseId as caseNumber if Submission data not provided
            submission["caseNumber"] = input.CaseId ?? "0";

            // Use input.Submission for additional fields if available
            if (input.Submission != null)
            {
                submission["caseCount"] = input.Submission.CaseCount ?? "-1.00";
                submission["adviser"] = input.Submission.Adviser ?? "";
                submission["caseStatusId"] = input.Submission.CaseStatusId ?? "8";
                submission["figPercentage"] = input.Submission.FigPercentage ?? "100";
                submission["investmentAmount"] = input.Submission.InvestmentAmount ?? input.ContactDetail?.InvestAmount ?? "0";
                submission["premiumId"] = input.Submission.PremiumId ?? $"OMP-{input.ContactDetail?.ContractNumber}";
                submission["productCode"] = input.Submission.ProductCode ?? "OMP";
                submission["productId"] = input.Submission.ProductId ?? "100";
                submission["salesCode"] = input.Submission.SalesCode ?? "630568";
                submission["splitCommissionId"] = input.Submission.SplitCommissionId ?? $"630568-{input.ContactDetail?.ContractNumber}";
                submission["statusReason"] = input.Submission.StatusReason ?? "Open";
                submission["team"] = input.Submission.Team ?? "Cheetah (AYMA)";
            }
            else
            {
                // Default values when no Submission data provided
                submission["caseCount"] = "-1.00";
                submission["adviser"] = "";
                submission["caseStatusId"] = "8";
                submission["figPercentage"] = "100";
                submission["investmentAmount"] = input.ContactDetail?.InvestAmount ?? "0";
                submission["premiumId"] = $"OMP-{input.ContactDetail?.ContractNumber}";
                submission["productCode"] = "OMP";
                submission["productId"] = "100";
                submission["salesCode"] = "630568";
                submission["splitCommissionId"] = $"630568-{input.ContactDetail?.ContractNumber}";
                submission["statusReason"] = "Open";
                submission["team"] = "Cheetah (AYMA)";
            }

            return new Dictionary<string, object>
            {
                ["submission"] = submission
            };
        }

        Task<ResponseData> IContractUpdateService.UpdateContract(RequestData input) => UpdateContract(input);
    }
}
