namespace E.S.RAS.AzureMessaging.Models;

public abstract class AzureServiceBusEvent
{
    public AzureServiceBusEvent(int identifier, string type)
    {
        Identifier = identifier;
        Type = type;
    }

    public AzureServiceBusEvent(string type)
    {
        Type = type;
    }

    public AzureServiceBusEvent()
    {
    }

    public int Identifier { get; set; }
    public string Type { get; set; }
    public bool Retry { get; set; }

    public AzureServiceBusEvent SetRetryCount(int retryCount)
    {
        return this;
    }
}