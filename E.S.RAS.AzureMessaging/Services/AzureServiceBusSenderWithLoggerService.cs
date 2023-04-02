using E.S.ApiClientHandler.Core;
using E.S.ApiClientHandler.Interfaces;
using E.S.Logging.Enums;
using E.S.Logging.Extensions;
using E.S.RAS.AzureMessaging.Constants;
using E.S.RAS.AzureMessaging.Interfaces;
using E.S.RAS.AzureMessaging.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RAS.AppToken.Interfaces;
using RAS.MessageQueue.Models;

namespace E.S.RAS.AzureMessaging.Services;

public class AzureServiceBusSenderWithLoggerService<TConfig> : IAzureServiceBusSenderWithLoggerService<TConfig>
    where TConfig : AzureServiceBusSenderConfiguration
{
    private const string LoggerSystem = LoggerConstant.SenderSystem;
    private readonly IApiIntegration _apiIntegration;
    private readonly IAppTokenService _appTokenService;
    private readonly TConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureServiceBusSenderWithLoggerService<TConfig>> _logger;

    public AzureServiceBusSenderWithLoggerService(TConfig config,
        ILogger<AzureServiceBusSenderWithLoggerService<TConfig>> logger,
        IAppTokenService appTokenService)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrWhiteSpace(_config.Domain))
            throw new ArgumentNullException(nameof(_config.Domain), "No domain specified");
        if (string.IsNullOrWhiteSpace(_config.HttpService))
            throw new ArgumentNullException(nameof(_config.HttpService), "No http service specified");
        if (string.IsNullOrWhiteSpace(_config.QueueName))
            throw new ArgumentNullException(nameof(_config.QueueName), "No queue name specified");

        _logger = logger;
        _appTokenService = appTokenService;
        _httpClient = new HttpClient();
        _apiIntegration = new ApiIntegration(_httpClient);
    }

    public async Task<bool> SendAsync<T>(T serviceBusEvent) where T : AzureServiceBusEvent
    {
        _logger.LogInformationOperation(LoggerStatusEnum.Start, LoggerSystem,
            serviceBusEvent.Type, serviceBusEvent.Identifier.ToString(),
            null, "Sending service bus message");

        var token = await _appTokenService.GetAppTokenAsync();

        var result = await SendAsync(
            serviceBusEvent,
            MessageQueueActionTypes.Insert,
            token);

        _logger.LogInformationOperation(LoggerStatusEnum.EndWithSucces, LoggerSystem,
            serviceBusEvent.Type, serviceBusEvent.Identifier.ToString(),
            null, "Complete sending service bus message");

        return result;
    }

    public Task<bool> SendAsync(object data, MessageQueueActionTypes actionType, string token = null)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        if (!_config.Enabled) return Task.FromResult(true);

        return SendAsync(data, actionType.ToString(), token);
    }

    public async Task<bool> SendAsync(object data, string actionType, string token = null)
    {
        _logger.LogInformationOperation(LoggerStatusEnum.InProgress, LoggerSystem,
            null, null,
            null, "Sending service bus message unknown");

        var packet = new AzureServiceBusPacket
        {
            Action = actionType.ToUpper(),
            QueueName = _config.QueueName,
            Domain = _config.Domain,
            Entity = data.GetType().Name.Replace("ViewModel", string.Empty).Replace("QueModel", string.Empty)
                .Replace("Model", string.Empty).Trim(),
            Payload = JsonConvert.SerializeObject(data, GetSerializerSettings(_config.FieldNameResolver))
        };

        var bearerToken = token ?? await _appTokenService.GetAppTokenAsync();
        if (!bearerToken.StartsWith("bearer", StringComparison.InvariantCultureIgnoreCase))
            bearerToken = "bearer " + bearerToken;

        try
        {
            var result = await _apiIntegration.ApiRequestBuilder.New()
                .WithMethod(HttpMethod.Post)
                .WithContent(packet)
                .AddHeader("Authorization", bearerToken)
                .WithUrl(_config.HttpService)
                .ExecuteAsync();

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogErrorOperation(LoggerStatusEnum.Error, LoggerSystem,
                null, null,
                null,
                "Failed sending service bus message",
                exception);

            throw;
        }
    }

    public void Dispose()
    {
        _apiIntegration.Dispose();
        _httpClient.Dispose();
    }

    private JsonSerializerSettings GetSerializerSettings(NameResolverType resolverType)
    {
        var serializerSettings = new JsonSerializerSettings();
        switch (resolverType)
        {
            case NameResolverType.CamelCase:
            {
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                break;
            }
            case NameResolverType.SnakeCase:
            {
                serializerSettings.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                };
                break;
            }
        }

        return serializerSettings;
    }
}