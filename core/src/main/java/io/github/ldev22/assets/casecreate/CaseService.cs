using Ade.Lambda.club51.Case.Create.Update.Models;
using Dapper;
using Snowflake.Data.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Amazon.Lambda.Core;
using System.Data;
using System.Text.Json;
using Azure;
using Ade.Lambda.club51.Case.Create.Update.Config;

namespace Ade.Lambda.club51.Case.Create.Update.Service
{
    public class CaseService
    {
        private readonly ILogger<CaseService> _logger;

        public CaseService(ILogger<CaseService> logger)
        {
            _logger = logger;
        }

        public async Task<string> CreateCase(SnowflakeDbConnection connection, EventModel input)
        {
            try
            {
             
                var caseId = await InsertCaseDetails(connection, input);

                Console.WriteLine($"Case processed successfully with ID: {caseId}");
                return caseId; 
            }
            catch (Exception ex)
            {
           
                _logger.LogError(ex, "Error processing case. Exception: {ErrorMessage}", ex.Message);

                throw new ApplicationException("Failed to create case", ex);
            }
        }


        private async Task<string> InsertCaseDetails(SnowflakeDbConnection connection, EventModel input)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true 
            };

            string jsonCase = JsonSerializer.Serialize(input, options);
            Console.WriteLine("Serialized input JSON: " + jsonCase);

            try
            {
                using (var command = connection.CreateCommand())
                {
                    Console.WriteLine("Creating database command...");

                    var _dbConfig = new DatabaseConfig();
                    string modString = "'" + jsonCase.Replace("'", "''") + "'"; // Escape single quotes for SQL
                    string edr_db = _dbConfig.Database;
                    string edr_schema = _dbConfig.Schema;
                    string edr_proc = "CREATEUPDATE_CASE";

                    string fullCommandText = $"CALL {edr_db}.{edr_schema}.{edr_proc}({modString})";
                    command.CommandText = fullCommandText;
                    command.CommandType = CommandType.Text;

                    Console.WriteLine("Executing Snowflake stored procedure:");
                    Console.WriteLine("CommandText: " + command.CommandText);
                    LambdaLogger.Log("Test deploy");
                    using var reader = command.ExecuteReader();
                    string response = "";

                    while (reader.Read())
                    {
                        response = reader[0]?.ToString();
                        Console.WriteLine($"Snowflake response: {response}");
                    }

                    if (string.IsNullOrWhiteSpace(response))
                    {
                        Console.WriteLine("No response returned from Snowflake procedure.");
                        throw new InvalidOperationException("No case ID returned from the stored procedure.");
                    }

                    Console.WriteLine("Returning response: " + response);
                    return response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while inserting case details: {ex.Message}");
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
                throw new ApplicationException("Failed to create case", ex);
            }
        }


    }
}
