using E.S.RAS.AzureMessaging.Models;
using Microsoft.Extensions.Logging;
using RAS.MessageQueue.Models;

namespace E.S.RAS.AzureMessaging.BackgroundServices;

public abstract class
    AzureServiceBusListenerWithLogger<T, TConfig> : AzureServiceBusListenerWithLoggerBase<T,
        TConfig>
    where T : AzureServiceBusEvent
    where TConfig : AzureServiceBusListenerConfiguration
{
    protected AzureServiceBusListenerWithLogger(TConfig config, ILogger logger,
        IServiceProvider serviceProvider)
        : base(config, logger, serviceProvider)
    {
    }
}