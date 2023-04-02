using Azure.Messaging.ServiceBus;
using E.S.Logging.Enums;
using E.S.Logging.Extensions;
using E.S.RAS.AzureMessaging.Constants;
using E.S.RAS.AzureMessaging.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RAS.MessageQueue.Models;
using RAS.MessageQueue.Services;

namespace E.S.RAS.AzureMessaging.BackgroundServices;

public abstract class
    AzureServiceBusListenerWithLoggerBase<T, TConfig> : AzureServiceBusListenerHostedService
    where T : AzureServiceBusEvent
    where TConfig : AzureServiceBusListenerConfiguration
{
    private const string LoggerSystem = LoggerConstant.ListenerSystem;
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public AzureServiceBusListenerWithLoggerBase(
        TConfig config,
        ILogger logger,
        IServiceProvider serviceProvider) : base(config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var message = GetMessage(args);

        try
        {
            var serviceBusEvent =
                JsonConvert.DeserializeObject<T>(message.Payload.ToString() ?? string.Empty);

            if (serviceBusEvent is null)
                throw new Exception("Failed to Deserialize Service Bus Event");

            try
            {
                _logger.LogInformationOperation(LoggerStatusEnum.Start, LoggerSystem,
                    serviceBusEvent.Type, serviceBusEvent.Identifier.ToString(),
                    null, "Processing service bus listener message");

                using (var scope = _serviceProvider.CreateScope())
                {
                    await ProcessMessageAsync(serviceBusEvent, scope.ServiceProvider);
                }

                _logger.LogInformationOperation(LoggerStatusEnum.EndWithSucces, LoggerSystem,
                    serviceBusEvent.Type, serviceBusEvent.Identifier.ToString(),
                    null, "Complete processing service bus listener message");
            }
            catch (Exception e)
            {
                _logger.LogErrorOperation(LoggerStatusEnum.EndWithError, LoggerSystem,
                    serviceBusEvent.Type, serviceBusEvent.Identifier.ToString(),
                    null, "Failed processing service bus listener message", e);

                if (serviceBusEvent.Retry)
                {
                    throw;
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogErrorOperation(LoggerStatusEnum.Error, LoggerSystem,
                null, null,
                null,
                "Error processing service bus listener message.",
                exception);
            
            throw;
        }
    }

    protected override Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogErrorOperation(LoggerStatusEnum.Error, LoggerSystem,
            null, args.EntityPath,
            null,
            "Error processing service bus listener message.",
            args.Exception);

        return Task.CompletedTask;
    }

    protected abstract Task ProcessMessageAsync(T azureServiceBusEvent, IServiceProvider serviceProvider);
}