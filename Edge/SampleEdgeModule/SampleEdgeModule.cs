//Remove nullable warnings: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives
#nullable disable

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using System.Text;
using System.Text.Json;

public class SampleEdgeModule : BackgroundService
{
    //SEE https://learn.microsoft.com/en-us/dotnet/core/extensions/logging
    private readonly ILogger<SampleEdgeModule> _logger;
    private readonly AppSettings _appSettings;
    private ModuleClient  _client;
    CancellationToken _cancellationToken;
    private int _messageRecievedCounter;

    int _messageCount  = 100;
    int _messageDelay = 1000;

    public SampleEdgeModule(ILogger<SampleEdgeModule> logger,IConfiguration config)
    {
        _logger = logger;
        _appSettings = config.GetRequiredSection("AppSettings")!.Get<AppSettings>();

    }

    //NOTE: This gets called automatically by the host
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
         _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        MqttTransportSettings mqttSetting = new(TransportType.Mqtt_Tcp_Only);
        ITransportSettings[] settings = { mqttSetting };

        // Open a connection to the Edge runtime
        _client = await ModuleClient.CreateFromEnvironmentAsync(settings);

        // Reconnect is not implented because we'll let docker restart the process when the connection is lost
        _client.SetConnectionStatusChangesHandler((status, reason) => 
            _logger.LogWarning("Connection changed: Status: {status} Reason: {reason}", status, reason));

        await _client.OpenAsync(_cancellationToken);

        _logger.LogInformation("IoT Hub module client initialized.");

        // Register callback to be called when a message is received by the module
        await _client.SetInputMessageHandlerAsync("input1", ReceiveMessageAsync, null);

       // Read the TemperatureThreshold value from the module twin's desired properties
        var moduleTwin = await _client.GetTwinAsync();
        await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, _client);
        
        //Init the Twin
        _messageCount = _appSettings.MessageCount; //Start with Settings values
        _messageDelay = _appSettings.MessageDelay; 
        Twin twin = await _client.GetTwinAsync();
        await InitDeviceTwin(twin,_cancellationToken);

        //Send some Messages
        await SendDeviceToCloudMessagesAsync(_appSettings.MessageCount,
                                            _appSettings.MessageDelay);

    }

     private async Task InitDeviceTwin(Twin twin,CancellationToken ct)
    {
       _logger.LogInformation("\tInitial twin value received:");
        _logger.LogInformation($"\t{twin.ToJson()}");

        Console.WriteLine("Sending sample start time as reported property");
        TwinCollection reportedProperties = new TwinCollection();
        reportedProperties["messageCount"] = _messageCount.ToString();
        reportedProperties["messageDelay"] = _messageDelay.ToString();
        await _client.UpdateReportedPropertiesAsync(reportedProperties);
        Console.WriteLine($"Use the IoT Hub Azure Portal or IoT Explorer utility to change the twin desired properties.");
        await _client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChangedAsync, ct);
        // This is how one can unsubscribe a callback for properties using a null callback handler.
        //await _client.SetDesiredPropertyUpdateCallbackAsync(null, null);
    }

    private async Task OnDesiredPropertyChangedAsync(TwinCollection desiredProperties, object userContext)
    {
        var reportedProperties = new TwinCollection();

        _logger.LogInformation("\tDesired properties requested:");
        _logger.LogInformation($"\t{desiredProperties.ToJson()}");

        // For the purpose of this sample, loop through all twin property write requests.
        foreach (KeyValuePair<string, object> desiredProperty in desiredProperties)
        {
            _logger.LogInformation($"Desired Property: {desiredProperty.Key} to {desiredProperty.Value}.");
        }
        //Json returns a long, so need to cast to int
        _messageDelay = (int)desiredProperties["messageDelay"].Value;
        _messageCount = (int)desiredProperties["messageCount"].Value;
        _logger.LogInformation($"Setting MessageDelay to {_messageDelay}.");
        _logger.LogInformation($"Setting MessageCount to {_messageCount}.");
        //Report Back
        reportedProperties["messageCount"] = _messageCount.ToString();
        reportedProperties["messageDelay"] = _messageDelay.ToString();
        await _client.UpdateReportedPropertiesAsync(reportedProperties);
    }  
        
    async Task<MessageResponse> ReceiveMessageAsync(Message message, object userContext)
    {
        int counterValue = Interlocked.Increment(ref _messageRecievedCounter);

        byte[] messageBytes = message.GetBytes();
        string messageString = Encoding.UTF8.GetString(messageBytes);
        _logger.LogInformation("Received message: {counterValue}, Body: [{messageString}]", counterValue, messageString);

        if (!string.IsNullOrEmpty(messageString))
        {
            using var pipeMessage = new Message(messageBytes);
            foreach (var prop in message.Properties)
            {
                pipeMessage.Properties.Add(prop.Key, prop.Value);
            }
            await _client!.SendEventAsync("output1", pipeMessage, _cancellationToken);
        
            _logger.LogInformation("Received message sent");
        }
        return MessageResponse.Completed;
    }
    // Async method to send simulated telemetry
    private async Task SendDeviceToCloudMessagesAsync(int messageCount, int messageDelay)
    {
        int currentTemperature = 0;
        try
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                for(int i = 0; i < messageCount;i++)
                {
                    int messageNumber = i+1;
                    currentTemperature++;
                    // Create JSON message
                    string messageBody = JsonSerializer.Serialize(
                        new
                        {
                            messageNumber = messageNumber,
                            temperature = currentTemperature
                        });
                    using var message = new Message(Encoding.ASCII.GetBytes(messageBody))
                    {
                        ContentType = "application/json",
                        ContentEncoding = "utf-8",
                    };

                    // Add a custom application property to the message.
                    // An IoT hub can filter on these properties without access to the message body.
                    message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");
                    if(currentTemperature > 30)
                        _logger.LogInformation($"temperatureAlert: {currentTemperature}");

                    // Send the telemetry message
                    await _client.SendEventAsync(message,_cancellationToken);
                    _logger.LogInformation($"{DateTime.Now} > Sending message: {messageBody}");

                    await Task.Delay(messageDelay);
                }
                return;
            }
        }
        catch (TaskCanceledException texc)
        { 
            _logger.LogInformation(texc.Message );  
        }
        catch (System.Exception exc)
        { 
            _logger.LogError(exc.Message + " : " + exc.StackTrace);  
        }
    }

    private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
    {
        _logger.LogInformation($"\nConnection status changed to {status}.");
        _logger.LogInformation($"Connection status changed reason is {reason}.\n");
    }

    private Task<MethodResponse> SetMessageDelayAsync(MethodRequest methodRequest, object userContext)
    {
        _logger.LogInformation($"\t *** {methodRequest.Name} was called.");
        _logger.LogInformation($"\t{methodRequest.DataAsJson}\n");
        MethodResponse retValue = new MethodResponse(methodRequest.Data, 200);
        return Task.FromResult(retValue);
    }

    Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
    {
    try
    {
        _logger.LogInformation("Desired property change:");
        _logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(desiredProperties));
        string temperatureThreshold;
        if (desiredProperties["TemperatureThreshold"]!=null)
            temperatureThreshold = desiredProperties["TemperatureThreshold"];

    }
    catch (AggregateException ex)
    {
        foreach (Exception exception in ex.InnerExceptions)
        {
            Console.WriteLine();
            Console.WriteLine("Error when receiving desired property: {0}", exception);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
    }
    return Task.CompletedTask;
}

}
