

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


    private async Task<ResponseData> UpdateContract(RequestData input)
    {

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = System.Text.Json.JsonSerializer.Serialize(input, options);
            var escaped = "'" + json.Replace("'", "''") + "'";
            var procName = "CREATEUPDATE_CASE";
            var connection = _connectionFactory.CreateConnection();
            var db = _dbConfig.DbEdrDb;
            var schema = _dbConfig.DbEdrSchemaClun51;
            using var command = connection.CreateCommand();
            command.CommandText = $"CALL {db}.{schema}.{procName}({escaped})";
            command.CommandType = CommandType.Text;

            LambdaLogger.Log($"INFO: Executing stored procedure: {db}.{schema}.{procName}");

            if (command is not System.Data.Common.DbCommand dbCommand)
            {
                throw new InvalidOperationException($"Command type '{command.GetType().FullName}' does not support async reader operations.");
            }

            using var reader = await dbCommand.ExecuteReaderAsync();

            ResponseData result = null;
            while (await reader.ReadAsync())
            {
                result = reader[0]?.ToString();
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                LambdaLogger.Log("WARN: Stored procedure returned no value; falling back to MERGE.");
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            ex = ex.InnerException ?? ex;
            LambdaLogger.Log($"WARN: Stored procedure path failed ({db}.{schema}.{procName}). Falling back to MERGE. Error: {ex.Message}");
            return null;
        }
    }


    Task<ResponseData> IContractUpdateService.UpdateContract(RequestData input)
    {
        return UpdateContract(input);
    }
}
