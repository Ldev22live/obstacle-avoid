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

            // Generate a new ContractDetailId if one does not exist
            var contractDetailId = string.IsNullOrWhiteSpace(input.ContactDetail?.ContractDetailId)
                ? Guid.NewGuid().ToString()
                : input.ContactDetail.ContractDetailId;

            using var command = connection.CreateCommand();

            command.CommandText = @"
        MERGE INTO FIFTYONECLUB_CONTRACTDETAIL t
        USING (
            SELECT
                :p1 AS CONTRACT_DETAIL_ID,
                :p2 AS CASE_ID,
                :p3 AS CONTRACT_NUMBER,
                :p4 AS PRODUCT_NAME,
                :p5 AS INVEST_TYPE,
                :p6 AS INVEST_AMOUNT,
                :p7 AS PAY_METHOD,
                :p8 AS COMM_ALLOWANCE,
                :p9 AS FP_FEE,
                :p10 AS NEG_COMM_ALLOWANCE,
                :p11 AS NEG_COMM_PERCENTAGE,
                :p12 AS CREATED_BY,
                :p13 AS MODIFIED_BY
        ) s
        ON t.CONTRACT_DETAIL_ID = s.CONTRACT_DETAIL_ID

        WHEN MATCHED THEN UPDATE SET
            CASE_ID = s.CASE_ID,
            CONTRACT_NUMBER = s.CONTRACT_NUMBER,
            PRODUCT_NAME = s.PRODUCT_NAME,
            INVEST_TYPE = s.INVEST_TYPE,
            INVEST_AMOUNT = s.INVEST_AMOUNT,
            PAY_METHOD = s.PAY_METHOD,
            COMM_ALLOWANCE = s.COMM_ALLOWANCE,
            FP_FEE = s.FP_FEE,
            NEG_COMM_ALLOWANCE = s.NEG_COMM_ALLOWANCE,
            NEG_COMM_PERCENTAGE = s.NEG_COMM_PERCENTAGE,
            MODIFIED_BY = s.MODIFIED_BY,
            MODIFIED_ON = CURRENT_TIMESTAMP()

        WHEN NOT MATCHED THEN INSERT (
            CONTRACT_DETAIL_ID,
            CASE_ID,
            CONTRACT_NUMBER,
            PRODUCT_NAME,
            INVEST_TYPE,
            INVEST_AMOUNT,
            PAY_METHOD,
            COMM_ALLOWANCE,
            FP_FEE,
            NEG_COMM_ALLOWANCE,
            NEG_COMM_PERCENTAGE,
            CREATED_BY,
            CREATED_ON
        )
        VALUES (
            s.CONTRACT_DETAIL_ID,
            s.CASE_ID,
            s.CONTRACT_NUMBER,
            s.PRODUCT_NAME,
            s.INVEST_TYPE,
            s.INVEST_AMOUNT,
            s.PAY_METHOD,
            s.COMM_ALLOWANCE,
            s.FP_FEE,
            s.NEG_COMM_ALLOWANCE,
            s.NEG_COMM_PERCENTAGE,
            s.CREATED_BY,
            CURRENT_TIMESTAMP()
        );
    ";

            AddPositionalParameter(command, contractDetailId);
            AddPositionalParameter(command, input.CaseId);
            AddPositionalParameter(command, input.ContactDetail?.ContractNumber);
            AddPositionalParameter(command, input.ContactDetail?.ProductName);
            AddPositionalParameter(command, input.ContactDetail?.InvestType);
            AddPositionalParameter(command, input.ContactDetail?.InvestAmount);
            AddPositionalParameter(command, input.ContactDetail?.PayMethod);
            AddPositionalParameter(command, input.ContactDetail?.CommAllowance);
            AddPositionalParameter(command, input.ContactDetail?.FpFee);
            AddPositionalParameter(command, input.ContactDetail?.NegCommAllowance);
            AddPositionalParameter(command, input.ContactDetail?.NegCommPercentage);
            AddPositionalParameter(command, input.ContactDetail?.CreatedBy);
            AddPositionalParameter(command, input.ContactDetail?.ModifiedBy);

            await command.ExecuteNonQueryAsync();

            response.Data.NewContractDetailId = contractDetailId;
        }
        catch (Exception ex)
        {
            ex = ex.InnerException ?? ex;
            LambdaLogger.Log($"ERROR: {ex.Message} | StackTrace: {ex.StackTrace}");

            try
            {
                if (ex.Message != null && ex.Message.IndexOf("invalid identifier", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    await DebugDescribeTableColumns("FIFTYONECLUB_CONTRACTDETAIL");
                }
            }
            catch
            {
            }

            throw;
        }

        LambdaLogger.Log($"INFO: UpdateContract completed. Response: {JsonConvert.SerializeObject(response)}");
        return response;
    }

    public async Task<IReadOnlyList<string>> DebugListAccessibleTables(int maxTables = 200)
    {
        var tables = new List<string>();

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SHOW TABLES";

            if (command is not System.Data.Common.DbCommand dbCommand)
            {
                throw new InvalidOperationException($"Command type '{command.GetType().FullName}' does not support async reader operations.");
            }

            using var reader = await dbCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (tables.Count >= maxTables)
                {
                    break;
                }

                var name = reader["name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    tables.Add(name);
                }
            }
        }
        catch (Exception ex)
        {
            ex = ex.InnerException ?? ex;
            LambdaLogger.Log($"ERROR: DebugListAccessibleTables failed - {ex.Message} | StackTrace: {ex.StackTrace}");
            throw;
        }

        LambdaLogger.Log($"INFO: DebugListAccessibleTables found {tables.Count} tables.");

        var batchSize = 25;
        for (var i = 0; i < tables.Count; i += batchSize)
        {
            var end = System.Math.Min(i + batchSize, tables.Count);
            var batch = new List<string>(end - i);
            for (var j = i; j < end; j++)
            {
                batch.Add(tables[j]);
            }

            LambdaLogger.Log($"INFO: DebugListAccessibleTables tables[{i}..{end - 1}]: {JsonConvert.SerializeObject(batch)}");
        }

        return tables;
    }

    public async Task<IReadOnlyList<string>> DebugDescribeTableColumns(string tableName, int maxColumns = 200)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
        }

        var columns = new List<string>();

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = $"DESCRIBE TABLE {tableName}";

            if (command is not System.Data.Common.DbCommand dbCommand)
            {
                throw new InvalidOperationException($"Command type '{command.GetType().FullName}' does not support async reader operations.");
            }

            using var reader = await dbCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (columns.Count >= maxColumns)
                {
                    break;
                }

                var name = reader["name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    columns.Add(name);
                }
            }
        }
        catch (Exception ex)
        {
            ex = ex.InnerException ?? ex;
            LambdaLogger.Log($"ERROR: DebugDescribeTableColumns failed - {ex.Message} | StackTrace: {ex.StackTrace}");
            throw;
        }

        LambdaLogger.Log($"INFO: DebugDescribeTableColumns({tableName}) found {columns.Count} columns.");

        var batchSize = 25;
        for (var i = 0; i < columns.Count; i += batchSize)
        {
            var end = System.Math.Min(i + batchSize, columns.Count);
            var batch = new List<string>(end - i);
            for (var j = i; j < end; j++)
            {
                batch.Add(columns[j]);
            }

            LambdaLogger.Log($"INFO: DebugDescribeTableColumns({tableName}) columns[{i}..{end - 1}]: {JsonConvert.SerializeObject(batch)}");
        }

        return columns;
    }

    // Helper method for positional parameters
    private void AddPositionalParameter(IDbCommand command, object value)
    {
        var param = command.CreateParameter();
        param.ParameterName = $"p{command.Parameters.Count + 1}";
        param.Value = value ?? DBNull.Value;
        command.Parameters.Add(param);
    }
}
