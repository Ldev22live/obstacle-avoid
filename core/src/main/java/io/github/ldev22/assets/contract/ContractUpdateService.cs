

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

            var db = _dbConfig.Database;
            var schema = _dbConfig.Schema;
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
            response.Data = new ResponseDataData
            {
                NewContractDetailId = procResult,
                NewCaseId = input.CaseId ?? string.Empty
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

