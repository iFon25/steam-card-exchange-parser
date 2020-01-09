using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamCardExchangeParser.Configuration;

namespace SteamCardExchangeParser
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(builder => builder.ClearProviders())
                .ConfigureServices(ConfigureServices);

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services
                .Configure<SteamCardExchangeOptions>(options =>
                    hostContext.Configuration.GetSection(nameof(SteamCardExchangeOptions)).Bind(options))
                .Configure<ServiceOptions>(options =>
                    hostContext.Configuration.GetSection(nameof(ServiceOptions)).Bind(options))
                .AddSingleton<IGameInfoLoader, GameInfoLoader>()
                .AddHostedService<Worker>();
        }
    }
}
