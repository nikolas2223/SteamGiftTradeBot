namespace AutoAcceptTrades
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;

    internal class Program
    {
        static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json", optional: false);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient();
                    services.AddSingleton<SteamClient>();
                    services.AddHostedService<TradeWorker>();
                })
                .Build()
                .Run();
        }
    }
}
