

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
            string? procResult = null;
            while (await reader.ReadAsync())
            {
                procResult = reader[0]?.ToString();
            }

            if (string.IsNullOrWhiteSpace(procResult))
            {
                throw new InvalidOperationException("No ID returned from the stored procedure.");
            }

            var verifiedCaseId = await TryGetCaseId(connection, db, schema, input.CaseId);

            response.Data.NewContractDetailId = procResult;
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

    private async Task<string?> TryGetContractDetailId(
        IDbConnection connection,
        string db,
        string schema,
        string? caseId,
        string? contractNumber)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            return null;
        }

        try
        {
            var columns = await GetTableColumns(connection, db, schema, "FIFTYONECLUB_CONTRACTDETAIL");
            if (columns.Count == 0)
            {
                LambdaLogger.Log("WARN: Could not read columns for FIFTYONECLUB_CONTRACTDETAIL.");
                return null;
            }

            string? idColumn = null;
            var preferredIdColumns = new[]
            {
                "CD_CONTRACTDETAIL_ID",
                "CD_CONTRACTDETAILID",
                "CONTRACTDETAIL_ID",
                "CONTRACT_DETAIL_ID",
                "CD_ID",
                "ID"
            };

            foreach (var c in preferredIdColumns)
            {
                if (columns.Contains(c))
                {
                    idColumn = c;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(idColumn))
            {
                LambdaLogger.Log("WARN: Could not determine contract detail ID column in FIFTYONECLUB_CONTRACTDETAIL.");
                return null;
            }

            var hasCaseId = columns.Contains("CD_CASE_ID");
            var hasEndDate = columns.Contains("CD_ENDDATE");
            var hasContractNumber = columns.Contains("CD_CONTRACTNUMBER");

            if (!hasCaseId || !hasEndDate)
            {
                LambdaLogger.Log("WARN: FIFTYONECLUB_CONTRACTDETAIL missing expected columns CD_CASE_ID and/or CD_ENDDATE.");
                return null;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT {idColumn}
FROM {db}.{schema}.FIFTYONECLUB_CONTRACTDETAIL
WHERE CD_CASE_ID = ?
  AND CD_ENDDATE = '9999-12-31'::DATE
{(hasContractNumber && !string.IsNullOrWhiteSpace(contractNumber) ? "  AND CD_CONTRACTNUMBER = ?\n" : string.Empty)}
LIMIT 1";
            command.CommandType = CommandType.Text;

            var pCaseId = command.CreateParameter();
            pCaseId.ParameterName = "CaseId";
            pCaseId.DbType = DbType.String;
            pCaseId.Value = caseId;
            command.Parameters.Add(pCaseId);

            if (hasContractNumber && !string.IsNullOrWhiteSpace(contractNumber))
            {
                var pContractNumber = command.CreateParameter();
                pContractNumber.ParameterName = "ContractNumber";
                pContractNumber.DbType = DbType.String;
                pContractNumber.Value = contractNumber;
                command.Parameters.Add(pContractNumber);
            }

            if (command is not System.Data.Common.DbCommand dbCommand)
            {
                throw new InvalidOperationException($"Command type '{command.GetType().FullName}' does not support async scalar operations.");
            }

            var result = await dbCommand.ExecuteScalarAsync();
            var contractDetailId = result == null || result == DBNull.Value ? null : result.ToString();

            if (string.IsNullOrWhiteSpace(contractDetailId))
            {
                LambdaLogger.Log($"WARN: No contract detail id found for caseId '{caseId}'.");
                return null;
            }

            return contractDetailId;
        }
        catch (Exception ex)
        {
            ex = ex.InnerException ?? ex;
            LambdaLogger.Log($"WARN: Contract detail id query failed for caseId '{caseId}': {ex.Message}");
            return null;
        }
    }

    private async Task<System.Collections.Generic.HashSet<string>> GetTableColumns(IDbConnection connection, string db, string schema, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $@"
SELECT COLUMN_NAME
FROM {db}.INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = ?
  AND TABLE_NAME = ?";
        command.CommandType = CommandType.Text;

        var pSchema = command.CreateParameter();
        pSchema.ParameterName = "Schema";
        pSchema.DbType = DbType.String;
        pSchema.Value = schema;
        command.Parameters.Add(pSchema);

        var pTable = command.CreateParameter();
        pTable.ParameterName = "Table";
        pTable.DbType = DbType.String;
        pTable.Value = table;
        command.Parameters.Add(pTable);

        if (command is not System.Data.Common.DbCommand dbCommand)
        {
            throw new InvalidOperationException($"Command type '{command.GetType().FullName}' does not support async reader operations.");
        }

        var result = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        using var reader = await dbCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var c = reader[0]?.ToString();
            if (!string.IsNullOrWhiteSpace(c))
            {
                result.Add(c);
            }
        }

        return result;
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
WHERE C.CASE_ID = ?
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

WARN: Case verification query failed for caseId '5cc85ce26524a1f98196b828009a1149652122c351bb8dd5397c1232e1140fcc': Error: SQL compilation error: error line 5 at position 18
Bind variable ? not set. SqlState: 42601, VendorCode: 2049, QueryId: 01c3654a-0309-b424-0001-ed421876ba6a
INFO: UpdateContract completed via stored procedure. Response: {"data":{"newContractDetailId":"Case Failed: ResultSet is empty or not prepared (call next() first)."},"isValid":true,"statusCode":200,"messages":[],"errors":[]}
INFO: Response received: {"data":{"newContractDetailId":"Case Failed: ResultSet is empty or not prepared (call next() first)."},"isValid":true,"statusCode":200,"messages":[],"errors":[]}
INFO: FunctionHandler execution completed.
