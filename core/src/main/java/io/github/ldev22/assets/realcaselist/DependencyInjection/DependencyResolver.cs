using Ade.Club51.Case.List.Abstractions;
using Ade.Club51.Case.List.Helpers;

using Ade.Club51.Case.List.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
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
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<ISnowflakeConnectionFactory, SnowflakeConnectionFactory>();
            services.AddScoped<IClubSearchService, ClubSearchService>();
        }

        private static IConfiguration GetConfiguration()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? Directory.GetCurrentDirectory();
            return new ConfigurationBuilder()
                        .SetBasePath(basePath)
                        .AddJsonFile("appsettings.json", optional: true)
                        .AddEnvironmentVariables()
                        .Build();
        }
    }
}
dotnet build