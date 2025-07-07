using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EcoguardPoller.Models;
using EcoguardPoller.Services;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        var env = context.HostingEnvironment;

        config.SetBasePath(Directory.GetCurrentDirectory());
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var appConfig = context.Configuration.Get<AppConfig>();

        services.AddSingleton(appConfig);
        services.AddSingleton<EcoGuardApiClient>();
        services.AddSingleton<MqttPublisher>();
        services.AddSingleton(new MeterReadingStore(appConfig.Database.Path));
        services.AddHostedService<AppService>();
    })
    .RunConsoleAsync();
