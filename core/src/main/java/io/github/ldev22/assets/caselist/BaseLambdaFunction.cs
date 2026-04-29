using Ade.Club51.Case.List.Validations.Response;
using OpenTelemetry.Trace;
using OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynatrace.OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using Dynatrace.OpenTelemetry.Instrumentation.AwsLambda;
using Ade.Club51.Case.List.Validations.Request;
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Ade.Club51.Case.List.Models.Request;
using Ade.Club51.Case.List.Models.Response;

namespace Ade.Club51.Case.List.Helpers
{

    public class BaseLambdaFunction
    {
        private readonly IConfiguration _config;
        public BaseLambdaFunction()
        {
            var serviceProvider = DependencyInjection.DependencyResolver.GetServiceProvider();
            _config = serviceProvider.GetService<IConfiguration>();
        }
        public void LogMessagesAndErrorsToConsole(ValidatableResponse response)
        {
            Console.WriteLine($"Response IsValid: {response.IsValid}.");

            response.Messages.ForEach(message =>
            {
                Console.WriteLine($"Response message: {message}");
            });

            response.Errors.ForEach(error =>
            {
                Console.WriteLine($"Response error: {error}");
            });

            response.ClearMessages();
        }

        public TracerProvider? GetDynatraceTraceProvider()
        {
            var useAwsLambdaConfigurations = bool.Parse(_config.GetValue<string>("useAwsLambdaConfigurations"));

            return useAwsLambdaConfigurations ? Sdk.CreateTracerProviderBuilder()
                        .AddDynatrace()
                        .AddAWSLambdaConfigurations(c => c.DisableAwsXRayContextExtraction = true)
                        .AddAWSInstrumentation(c => c.SuppressDownstreamInstrumentation = true)
                        .AddDynatraceAwsSdkInjection()
                        .Build() : Sdk.CreateTracerProviderBuilder()
                        .AddDynatrace()
                        .AddAWSInstrumentation(c => c.SuppressDownstreamInstrumentation = true)
                        .AddDynatraceAwsSdkInjection()
                        .Build();
        }

        // Helper method to deserialize the incoming JSON request
        public RequestData DeserializeRequest(JObject request)
        {
            return JsonConvert.DeserializeObject<RequestData>(JsonConvert.SerializeObject(request));
        }
  
        // Helper method to validate the request and add errors to the response
        public void ValidateRequest(RequestData request, GenericValidatableResponse<List<ClientResponse>> response)
        {
            var validator = new RequestValidator();
            var validationResult = validator.Validate(request);

            foreach (var error in validationResult.Errors)
            {
                response.AddError(error.ErrorMessage);
            }
        }

    }
}

