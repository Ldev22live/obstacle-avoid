using Amazon.Lambda.Core;
using Dynatrace.OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry;
using Dynatrace.OpenTelemetry.Instrumentation.AwsLambda;
using OpenTelemetry.Instrumentation.AWSLambda;

namespace Ade.Club51.Case.Details.Helpers
{
    public static class DynatraceHelper
    {
        public static TracerProvider? CreateTracerProvider(bool useAwsLambdaConfigurations)
        {
            LambdaLogger.Log("INFO: Initializing Dynatrace Tracer Provider...");

            var tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
                .AddDynatrace()
                .AddAWSInstrumentation(c => c.SuppressDownstreamInstrumentation = true)
                .AddDynatraceAwsSdkInjection();

            if (useAwsLambdaConfigurations)
            {
                LambdaLogger.Log("INFO: Adding AWS Lambda configurations to Dynatrace.");
                tracerProviderBuilder.AddAWSLambdaConfigurations(c => c.DisableAwsXRayContextExtraction = true);
            }

            LambdaLogger.Log("INFO: Dynatrace Tracer Provider setup complete.");
            return tracerProviderBuilder.Build();
        }

        // Helper for initializing Dynatrace logging
        public static void InitDynatraceLogging()
        {
            LambdaLogger.Log("INFO: Initializing Dynatrace logging...");
            DynatraceSetup.InitializeLogging();
            LambdaLogger.Log("INFO: Dynatrace logging initialized successfully.");
        }

        // Helper method to get AWS Lambda configurations
        public static bool GetAwsLambdaConfigurations()
        {
            bool useAwsLambdaConfigurations = bool.Parse(Environment.GetEnvironmentVariable("DT_USE_AWS_LAMBDA_CONFIGURATIONS") ?? "false");
            return useAwsLambdaConfigurations;
        }
    }
}
