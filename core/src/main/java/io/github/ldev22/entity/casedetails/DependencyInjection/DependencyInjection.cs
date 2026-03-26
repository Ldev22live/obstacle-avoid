using Ade.Club51.Case.Details.Config;
using Ade.Club51.Case.Details.Helpers;
using Ade.Club51.Case.Details.Interface;
using Ade.Club51.Case.Details.Services;
using Ade.Club51.Case.Details.Validations;
using Microsoft.Extensions.DependencyInjection;

namespace Ade.Club51.Case.Details.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection ConfigureServices(DatabaseConfig dbConfig)
        {
            var services = new ServiceCollection();

            services.AddSingleton(dbConfig);
            services.AddTransient<ISnowflakeConnectionFactory, SnowflakeConnectionFactory>();
            services.AddTransient<IRequestValidator, RequestValidator>();

            return services;
        }
    }
}
