using Ade.Club51.Case.List.Abstractions;
using Ade.Club51.Case.List.Helpers;

using Ade.Club51.Case.List.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.List.DependencyInjection
{
    public class DependencyResolver
    {
        public static IServiceProvider GetServiceProvider()
        {
            var configuration = GetConfiguration();
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, configuration);
            return serviceCollection.BuildServiceProvider();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IClubSearchService, ClubSearchService>();
            services.AddSingleton<ISnowflakeConnectionFactory, SnowflakeConnectionFactory>();
            services.AddSingleton<IConfiguration>(configuration);
        }

        private static IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddEnvironmentVariables()
                        .Build();
        }
    }
}
