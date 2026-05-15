using Ade.Club51.Case.List.Abstractions;
using Ade.Club51.Case.List.DependencyInjection;
using Ade.Club51.Case.List.Helpers;
using Ade.Club51.Case.List.Models.Request;
using Ade.Club51.Case.List.Validations.Request;
using Ade.Club51.Case.List.Validations.Response;
using Amazon.Lambda.Core;
using Dynatrace.OpenTelemetry.Instrumentation.AwsLambda;
using Dynatrace.OpenTelemetry;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;
using Ade.Club51.Case.List.Models.Response;
using Azure;
using Amazon.Runtime.Internal;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]


namespace Ade.Club51.Case.List
{

    public class Function : BaseLambdaFunction
    {
        private readonly TracerProvider _tracerProvider;
        private readonly IClubSearchService _clubSearch;
        public Function() : this(DependencyResolver.GetServiceProvider().GetService<IClubSearchService>())
        {
            LambdaLogger.Log("Initialization started...");
            try
            {
                DynatraceSetup.InitializeLogging();
                _tracerProvider = GetDynatraceTraceProvider();
            }
            catch (Exception ex)
            {
                LambdaLogger.Log($"Dynatrace initialization skipped: {ex.Message}");
            }
            LambdaLogger.Log("Initialization completed...");
        }

        // Constructor with injected service for easier testing
        public Function(IClubSearchService clubSearch)
        {
            _clubSearch = clubSearch;
        }
        public async Task<GenericValidatableResponse<List<ClientResponse>>> FunctionHandler(JObject input, ILambdaContext context)
        {
            LambdaLogger.Log($"Lambda ARN: {context.InvokedFunctionArn}");

            // Skip tracing if tracerProvider is null (local testing)
            if (_tracerProvider == null)
            {
                LambdaLogger.Log("Tracing disabled - running without Dynatrace");
                return await FunctionHandlerInternalAsync(input, context);
            }

            // Extract propagation context from the ILambdaContext
            var propagationContext = AwsLambdaHelpers.ExtractPropagationContext(context);

            LambdaLogger.Log("Returning Wrapper...");
            // Trace the execution of FunctionHandlerInternalAsync using AWSLambdaWrapper
            return await AWSLambdaWrapper.TraceAsync(
            _tracerProvider,
            FunctionHandlerInternalAsync,
            input,
            context,
                propagationContext.ActivityContext
            );
        }

        // Internal function to handle the main logic
        public async Task<GenericValidatableResponse<List<ClientResponse>>> FunctionHandlerInternalAsync(JObject request, ILambdaContext context)
        {
            var response = new GenericValidatableResponse<List<ClientResponse>>(new());
            var requestData = JsonConvert.DeserializeObject<RequestData>(JsonConvert.SerializeObject(request));
            // Deserialize the input JSON into object
            LambdaLogger.Log("Request: " + JsonConvert.SerializeObject(request));
            var payload = DeserializeRequest(request);

            try
            {
                ValidateRequest(payload, response);

                if (response.IsValid)
                {

                    payload.PageNum = payload.PageNum == 0 ? 1 : payload.PageNum;
                    payload.PageSize = payload.PageSize == 0 ? 10000 : payload.PageSize;

                    var serviceResponse = await _clubSearch.GetCaseList(payload);
                    response.Data = response.MergeResponsesAndReturnEntity(serviceResponse);
                    response.Total = serviceResponse.Total;
                    response.Page = payload.PageNum;
                    response.PageSize = (payload.PageSize == 0) ? serviceResponse.Total : payload.PageSize;
  
                    LambdaLogger.Log($"Response data: {JsonConvert.SerializeObject(response.Data)}");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error occurred while retrieving caselist. Error: {ex.GetBaseException().Message}";
                LambdaLogger.Log(errorMessage);
                response.AddError(errorMessage);
            }
            response.SetStatusCode();
            return response;
        }
    }
}