

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

            response.Data.newContractDetailId = newId;
            response.IsValid = true;
            response.StatusCode = 200;
            //after SP execute similar query
            var caseId = request.Submission.CaseNumber;
            var salesCode = request.Submission.SalesCode;
            var productId = request.Submission.New51ClubsubmissionId;

            LambdaLogger.Log($"INFO: Parameters for verification query — CaseId: {caseId}, SalesCode: {salesCode}, ProductId: {productId}");

            var queryBuilder = new StringBuilder();
            queryBuilder.AppendLine($"SELECT SUBMISSIONS_ID FROM {db}.CLUB51.FIFTYONECLUB_SUBMISSIONS");
            queryBuilder.AppendLine("WHERE SUBMISSIONS_ENDDATE = '9999-12-31'");
            queryBuilder.AppendLine("AND SUBMISSIONS_ISREVERSAL = 0");
            queryBuilder.AppendLine($"AND SUBMISSIONS_CASE_ID = '{caseId?.Replace("'", "''")}'");
            queryBuilder.AppendLine("AND SUBMISSIONS_ISLATEST = 1");
            queryBuilder.AppendLine($"AND SUBMISSIONS_SALESCODE = '{salesCode?.Replace("'", "''")}'");

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

    Task<ResponseData> IContractUpdateService.UpdateContract(RequestData input) => UpdateContract(input);
}
