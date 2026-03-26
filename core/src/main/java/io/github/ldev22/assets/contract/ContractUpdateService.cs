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
                :p1 AS CD_ID,
                :p2 AS CD_CASE_ID,
                :p3 AS CD_CONTRACTNUMBER,
                :p4 AS CD_PRODUCT_ID,
                :p5 AS CD_INVESTTYPE,
                :p6 AS CD_INVESTAMOUNT,
                :p7 AS CD_PM_ID,
                :p8 AS CD_COMMALLOWANCE,
                :p9 AS CD_FPFEE,
                :p10 AS CD_NEGCOMMALLOWANCE,
                :p11 AS CD_NEGCOMMPERCENTAGE,
                :p12 AS CD_CREATEDBY,
                :p13 AS CD_MODIFIEDBY
        ) s
        ON t.CD_ID = s.CD_ID

        WHEN MATCHED THEN UPDATE SET
            CD_CASE_ID = s.CD_CASE_ID,
            CD_CONTRACTNUMBER = s.CD_CONTRACTNUMBER,
            CD_PRODUCT_ID = s.CD_PRODUCT_ID,
            CD_INVESTTYPE = s.CD_INVESTTYPE,
            CD_INVESTAMOUNT = s.CD_INVESTAMOUNT,
            CD_PM_ID = s.CD_PM_ID,
            CD_COMMALLOWANCE = s.CD_COMMALLOWANCE,
            CD_FPFEE = s.CD_FPFEE,
            CD_NEGCOMMALLOWANCE = s.CD_NEGCOMMALLOWANCE,
            CD_NEGCOMMPERCENTAGE = s.CD_NEGCOMMPERCENTAGE,
            CD_MODIFIEDBY = s.CD_MODIFIEDBY,
            CD_MODIFIED = CURRENT_TIMESTAMP()

        WHEN NOT MATCHED THEN INSERT (
            CD_ID,
            CD_CASE_ID,
            CD_CONTRACTNUMBER,
            CD_PRODUCT_ID,
            CD_INVESTTYPE,
            CD_INVESTAMOUNT,
            CD_PM_ID,
            CD_COMMALLOWANCE,
            CD_FPFEE,
            CD_NEGCOMMALLOWANCE,
            CD_NEGCOMMPERCENTAGE,
            CD_CREATEDBY,
            CD_CREATEDON,
            CD_MODIFIEDBY,
            CD_MODIFIED
        )
        VALUES (
            s.CD_ID,
            s.CD_CASE_ID,
            s.CD_CONTRACTNUMBER,
            s.CD_PRODUCT_ID,
            s.CD_INVESTTYPE,
            s.CD_INVESTAMOUNT,
            s.CD_PM_ID,
            s.CD_COMMALLOWANCE,
            s.CD_FPFEE,
            s.CD_NEGCOMMALLOWANCE,
            s.CD_NEGCOMMPERCENTAGE,
            s.CD_CREATEDBY,
            CURRENT_TIMESTAMP(),
            s.CD_MODIFIEDBY,
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
