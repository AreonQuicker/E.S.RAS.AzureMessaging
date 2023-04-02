using E.S.RAS.AzureMessaging.Models;
using RAS.MessageQueue.Models;

namespace E.S.RAS.AzureMessaging.Interfaces;

public interface IAzureServiceBusSenderWithLoggerService<TConfig> : IDisposable
    where TConfig : AzureServiceBusSenderConfiguration
{
    Task<bool> SendAsync<T>(T serviceBusEvent) where T : AzureServiceBusEvent;
    Task<bool> SendAsync(object data, MessageQueueActionTypes actionType, string token = null);
    Task<bool> SendAsync(object data, string actionType, string token = null);
}