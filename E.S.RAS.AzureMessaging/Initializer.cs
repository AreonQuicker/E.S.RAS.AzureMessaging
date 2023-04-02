using E.S.RAS.AzureMessaging.BackgroundServices;
using E.S.RAS.AzureMessaging.Interfaces;
using E.S.RAS.AzureMessaging.Models;
using E.S.RAS.AzureMessaging.Services;
using Microsoft.Extensions.DependencyInjection;
using RAS.MessageQueue.Models;

namespace E.S.RAS.AzureMessaging;

public static class Initializer
{
    public static void AddAzureServiceBusSenderWithLogger<TConfig>(this IServiceCollection services,
        TConfig configuration)
        where TConfig : AzureServiceBusSenderConfiguration
    {
        services.AddSingleton<TConfig, TConfig>(_ =>
            configuration);

        services
            .AddTransient<IAzureServiceBusSenderWithLoggerService<TConfig>,
                AzureServiceBusSenderWithLoggerService<TConfig>>();
    }

    public static void AddAzureServiceBusListenerAndSenderWithLogger<TListener, TConfig, TSendConfig, TPacket>(
        this IServiceCollection services,
        TConfig listenerConfiguration,
        TSendConfig sendConfiguration)
        where TListener : AzureServiceBusListenerWithLogger<TPacket, TConfig>
        where TConfig : AzureServiceBusListenerConfiguration
        where TSendConfig : AzureServiceBusSenderConfiguration
        where TPacket : AzureServiceBusEvent
    {
        services.AddAzureServiceBusSenderWithLogger<TSendConfig>(sendConfiguration);
        services.AddAzureServiceBusListenerWithLogger<TListener, TConfig, TPacket>(listenerConfiguration);
    }

    public static void AddAzureServiceBusListenerWithLogger<TListener, TConfig, TPacket>(
        this IServiceCollection services,
        TConfig configuration)
        where TListener : AzureServiceBusListenerWithLogger<TPacket, TConfig>
        where TConfig : AzureServiceBusListenerConfiguration
        where TPacket : AzureServiceBusEvent
    {
        services.AddSingleton<TConfig, TConfig>(_ =>
            configuration);

        services.AddHostedService<TListener>();
    }
}