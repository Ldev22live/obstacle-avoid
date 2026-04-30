using Ade.Lambda.club51.Case.Create.Update.Config;
using Ade.Lambda.club51.Case.Create.Update.Models;
using Ade.Lambda.club51.Case.Create.Update.Service;
using Amazon.Lambda.Core;
using Dynatrace.OpenTelemetry;
using Dynatrace.OpenTelemetry.Instrumentation.AwsLambda;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]


namespace Ade.Lambda.club51.Case.Create.Update
{
    public class Function
    {
        private readonly CaseService _caseService;
        private readonly DatabaseConfig _dbConfig;
        private readonly TracerProvider _tracerProvider;
        public Function()
        {
            _dbConfig = new DatabaseConfig(); 

    
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger<CaseService>();

    
            _caseService = new CaseService(logger);
  
        }

        public Function(CaseService caseService, DatabaseConfig dbConfig)
        {
            _caseService = caseService ?? throw new ArgumentNullException(nameof(caseService));
            _dbConfig = dbConfig ?? throw new ArgumentNullException(nameof(dbConfig));
        }
        public TracerProvider? GetDynatraceTraceProvider()
        {
      
            bool useAwsLambdaConfigurations = bool.Parse(Environment.GetEnvironmentVariable("DT_USE_AWS_LAMBDA_CONFIGURATIONS") ?? "false");

            var tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
                .AddDynatrace()
                .AddAWSInstrumentation(c => c.SuppressDownstreamInstrumentation = true)
                .AddDynatraceAwsSdkInjection();

            if (useAwsLambdaConfigurations)
            {
                tracerProviderBuilder.AddAWSLambdaConfigurations(c => c.DisableAwsXRayContextExtraction = true);
            }

            return tracerProviderBuilder.Build();
        }
      
       public async Task<CaseResponse> FunctionHandler(EventModel input, ILambdaContext context)
        {
            LambdaLogger.Log($"Lambda invoked: ARN = {context.InvokedFunctionArn}");
            LambdaLogger.Log("Extracting propagation context...");

            var propagationContext = AwsLambdaHelpers.ExtractPropagationContext(context);
            LambdaLogger.Log("Processing request...");

            return await AWSLambdaWrapper.TraceAsync(
                _tracerProvider,
                FunctionHandlerInternalAsync,
                input,
                context,
                propagationContext.ActivityContext
            );
        }

      
     public async Task<CaseResponse> FunctionHandlerInternalAsync(EventModel input, ILambdaContext context)
        {
            context.Logger.LogLine("FunctionHandler invoked.");

     
            context.Logger.LogLine($"Input received: {System.Text.Json.JsonSerializer.Serialize(input)}");

            var response = InputValidator.ValidateInput(input);

        
            if (!response.IsValid)
            {
                context.Logger.LogLine("Input validation failed.");
                return response;
            }
     
            context.Logger.LogLine($"Database configuration: Server={_dbConfig.Server}, Database={_dbConfig.Database}, UserId={_dbConfig.UserId}");


            context.Logger.LogLine($"Connection string created (sensitive data masked).");
            var connectionString = $"account={_dbConfig.Account};user={_dbConfig.UserId};password={_dbConfig.Password};scheme=https;host={_dbConfig.Server};port={_dbConfig.Port};db={_dbConfig.Database};schema={_dbConfig.Schema};insecureMode=true";
            context.Logger.LogLine($"Connection string:{connectionString}");
            try
            {
                using (var connection = new Snowflake.Data.Client.SnowflakeDbConnection { ConnectionString = connectionString })
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Connection to Snowflake successful.");
                    LambdaLogger.Log("Creating case...");

                    var caseId = await _caseService.CreateCase(connection, input);                   
                    var evenType = input.EventType.ToString();

             
                    response.Data = new CaseData
                    {
                        CaseId = caseId,
                        Result = $"{evenType}"
                    };
                    response.IsValid = true;
                    response.StatusCode = 200;

                    LambdaLogger.Log($"Case created successfully with ID: {caseId}");
                    LambdaLogger.Log($"Response prepared: {System.Text.Json.JsonSerializer.Serialize(response)}");
                }
            }
            catch (Exception ex)
            {
           
                context.Logger.LogLine($"Exception caught: {ex.Message}");
                LambdaLogger.Log($"Exception caught: {ex.Message}");

                response.Data = new CaseData(); // Provide a default value for data
                response.IsValid = false;
                response.StatusCode = 500;
                response.Errors = new List<string> { ex.Message }; // Include the error message
            }

            context.Logger.LogLine($"Response prepared: {System.Text.Json.JsonSerializer.Serialize(response)}");

            return response;

        }


    }
}