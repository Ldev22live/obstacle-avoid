

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

        var procName = Environment.GetEnvironmentVariable("CONTRACTDETAIL_PROC") ?? "CREATEUPDATE_CASE";
        var db = _dbConfig.Database;
        var schema = _dbConfig.Schema;

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

            using var command = connection.CreateCommand();
            command.CommandText = $"CALL {db}.{schema}.{procName}({escaped})";
            command.CommandType = CommandType.Text;

            LambdaLogger.Log($"INFO: Executing stored procedure: {db}.{schema}.{procName}");

            if (command is not System.Data.Common.DbCommand dbCommand)
            {
                throw new InvalidOperationException($"Command type '{command.GetType().FullName}' does not support async reader operations.");
            }

            using var reader = await dbCommand.ExecuteReaderAsync();

            string? newId = null;
            while (await reader.ReadAsync())
            {
                newId = reader[0]?.ToString();
            }

            if (string.IsNullOrWhiteSpace(newId))
            {
                throw new InvalidOperationException("No ID returned from the stored procedure.");
            }

            var verifiedCaseId = await TryGetCaseId(connection, db, schema, input.CaseId);
            response.Data.NewContractDetailId = verifiedCaseId ?? input.CaseId;
            response.Data.NewCaseId = verifiedCaseId ?? string.Empty;
            response.IsValid = true;
            response.StatusCode = 200;

            LambdaLogger.Log($"INFO: UpdateContract completed via stored procedure. Response: {JsonConvert.SerializeObject(response)}");
            return response;
        }
        catch (Exception ex)
        {
            ex = ex.InnerException ?? ex;
            LambdaLogger.Log($"ERROR: UpdateContract failed ({db}.{schema}.{procName}). Error: {ex.Message} | StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task<string?> TryGetCaseId(IDbConnection connection, string db, string schema, string? caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            return null;
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT
    C.CASE_ID AS CaseId
FROM {db}.{schema}.FIFTYONECLUB_CASE C
WHERE C.CASE_ID = :CaseId
  AND C.CASE_ENDDATE = '9999-12-31'::DATE
LIMIT 1";
            command.CommandType = CommandType.Text;

            var p = command.CreateParameter();
            p.ParameterName = "CaseId";
            p.DbType = DbType.String;
            p.Value = caseId;
            command.Parameters.Add(p);

            if (command is not System.Data.Common.DbCommand dbCommand)
            {
                throw new InvalidOperationException($"Command type '{command.GetType().FullName}' does not support async scalar operations.");
            }

            var result = await dbCommand.ExecuteScalarAsync();
            var verifiedCaseId = result == null || result == DBNull.Value ? null : result.ToString();

            if (string.IsNullOrWhiteSpace(verifiedCaseId))
            {
                LambdaLogger.Log($"WARN: Case verification query did not find CASE_ID for caseId '{caseId}'.");
                return null;
            }

            return verifiedCaseId;
        }
        catch (Exception ex)
        {
            ex = ex.InnerException ?? ex;
            LambdaLogger.Log($"WARN: Case verification query failed for caseId '{caseId}': {ex.Message}");
            return null;
        }
    }

    Task<ResponseData> IContractUpdateService.UpdateContract(RequestData input) => UpdateContract(input);
}
