using Amazon.Lambda.Core;
using OpenTelemetry.Trace;
using Ade.Club51.Case.Details.Config;
using Ade.Club51.Case.Details.Helpers;
using Ade.Club51.Case.Details.Interface;
using Ade.Club51.Case.Details.Services;
using Ade.Club51.Case.Details.Validations;
using Microsoft.Extensions.DependencyInjection;
using Ade.Club51.Case.Details.Models;
//using Dynatrace.OpenTelemetry.Instrumentation.AwsLambda;
//using Dynatrace.OpenTelemetry;
using Newtonsoft.Json;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry;
using Ade.Club51.Case.Details.Services.Ade.Club51.Case.Details.Services;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Ade.Club51.Case.Details
{

    public class Function
    {
        private readonly TracerProvider? _tracerProvider;
        private readonly ICaseDetailsService _caseDetailsService;
        private readonly IRequestValidator _requestValidator;
        private readonly DatabaseConfig _dbConfig;

        public Function()
        {
            bool useAwsLambdaConfigurations = bool.Parse(Environment.GetEnvironmentVariable("DT_USE_AWS_LAMBDA_CONFIGURATIONS") ?? "false");
            LambdaLogger.Log("INFO: Starting Function constructor...");

            // Initialize Dynatrace TracerProvider using the helper
            _tracerProvider = DynatraceHelper.CreateTracerProvider(useAwsLambdaConfigurations)
                ?? throw new NullReferenceException(nameof(TracerProvider));
            LambdaLogger.Log("INFO: Dynatrace Tracer Provider initialized successfully.");

            DynatraceHelper.InitDynatraceLogging();

            LambdaLogger.Log("INFO: Initializing DatabaseConfig...");
            _dbConfig = new DatabaseConfig();
            LambdaLogger.Log("INFO: DatabaseConfig initialized.");

            LambdaLogger.Log("INFO: Initializing SnowflakeConnectionFactory...");
            var connectionFactory = new SnowflakeConnectionFactory(_dbConfig);


            _caseDetailsService = new CaseDetailsService(connectionFactory, _dbConfig);
            _requestValidator = new RequestValidator();

            LambdaLogger.Log("INFO: Function constructor completed.");
        }

        public Function(ICaseDetailsService caseDetailsService, IRequestValidator requestValidator, DatabaseConfig dbConfig)
        {
            LambdaLogger.Log("INFO: Overloaded Function constructor invoked.");

            _caseDetailsService = caseDetailsService ?? throw new ArgumentNullException(nameof(caseDetailsService));
            _requestValidator = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
            _dbConfig = dbConfig ?? throw new ArgumentNullException(nameof(dbConfig));

            LambdaLogger.Log("INFO: Dependencies injected successfully.");
        }

        public async Task<ResponseData> FunctionHandler(RequestData requestData, ILambdaContext context)
        {
            LambdaLogger.Log($"INFO: Lambda function invoked. ARN: {context.InvokedFunctionArn}");
            LambdaLogger.Log("INFO: Extracting AWS Lambda context for Dynatrace tracing...");

            //var propagationContext = AwsLambdaHelpers.ExtractPropagationContext(context);

            LambdaLogger.Log("INFO: Starting request processing...");

            return await AWSLambdaWrapper.TraceAsync(
                _tracerProvider,
                FunctionHandlerInternalAsync,
                requestData,
                context
                //propagationContext.ActivityContext
            );
        }

        public async Task<ResponseData> FunctionHandlerInternalAsync(RequestData input, ILambdaContext context)
        {
            LambdaLogger.Log("INFO: FunctionHandlerInternalAsync invoked.");

            // Ensure dependencies are initialized
            if (_caseDetailsService == null || _requestValidator == null)
            {
                LambdaLogger.Log("ERROR: Dependencies (_adviserService or _requestValidator) are not initialized.");
                return new ResponseData
                {
                    Data = new Data(),
                    IsValid = false,
                    StatusCode = 500,
                    Errors = new List<string> { "Internal Server Error: Missing Dependencies" }
                };
            }

            // Log input request
            if (input == null)
            {
                LambdaLogger.Log("ERROR: Input is null.");
                return new ResponseData
                {
                    Data = new Data(),
                    IsValid = false,
                    StatusCode = 400,
                    Errors = new List<string> { "Request data cannot be null." }
                };
            }

            LambdaLogger.Log($"INFO: Received input: {JsonConvert.SerializeObject(input)}");

            // Validate input
            LambdaLogger.Log("INFO: Starting input validation...");
            var validationResult = _requestValidator.Validate(input);

            if (!validationResult.IsValid)
            {
                LambdaLogger.Log($"ERROR: Validation failed. Errors: {JsonConvert.SerializeObject(validationResult.Errors)}");
                return new ResponseData
                {
                    Data = new Data(),
                    IsValid = false,
                    StatusCode = 400,
                    Errors = validationResult.Errors
                };
            }
            LambdaLogger.Log("INFO: Input validation passed successfully.");

            try
            {
                LambdaLogger.Log("INFO: Calling _adviserService.GetAdviserDetails...");
                var response = await _caseDetailsService.GetCaseDetails(input);

                if (response == null)
                {
                    LambdaLogger.Log("ERROR: Response from _adviserService.GetAdviserDetails is null.");
                    return new ResponseData
                    {
                        Data = new Data(),
                        IsValid = false,
                        StatusCode = 500,
                        Errors = new List<string> { "Service response was null." }
                    };
                }

                LambdaLogger.Log($"INFO: Response received: {JsonConvert.SerializeObject(response)}");
                return response;
            }
            catch (Exception ex)
            {
                LambdaLogger.Log($"ERROR: Exception occurred in _adviserService.GetAdviserDetails - {ex.Message} | StackTrace: {ex.StackTrace}");
                return new ResponseData
                {
                    Data = new Data(),
                    IsValid = false,
                    StatusCode = 500,
                    Errors = new List<string> { "Internal Server Error: Service failure" }
                };
            }
            finally
            {
                LambdaLogger.Log("INFO: FunctionHandler execution completed.");
            }
        }
    }
}