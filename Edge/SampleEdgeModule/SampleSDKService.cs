//Remove nullable warnings: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives
#nullable disable

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

public class SampleSDKService : BackgroundService
{
    //SEE https://learn.microsoft.com/en-us/dotnet/core/extensions/logging
    private readonly ILogger<SampleSDKService> _logger;
    private readonly AppSettings _appSettings;
    private readonly DeviceClient _client;

    int _messageCount  = 100;
    int _messageDelay = 1000;

    public SampleSDKService(ILogger<SampleSDKService> logger,IConfiguration config)
    {
        _logger = logger;
        _appSettings = config.GetRequiredSection("AppSettings")!.Get<AppSettings>();

        try
        {
           _client = DeviceClient.CreateFromConnectionString(_appSettings.DeviceConnectionString);
  
        }
        catch (System.Exception exc)
        { 
            _logger.LogInformation(exc.Message + " : " + exc.StackTrace);  
    }
    }

    //NOTE: This gets called automatically by the host
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
         _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

         _client.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);
         await _client.SetMethodHandlerAsync("SetMessageDelay", DirectMethodExampleAsync, null,stoppingToken);
         await _client.SetReceiveMessageHandlerAsync(OnC2DMessageReceivedAsync, _client);
                          await _client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChangedAsync, null);

        //Init the Twin
        _messageCount = _appSettings.MessageCount; //Start with Settings values
        _messageDelay = _appSettings.MessageDelay; 
        Twin twin = await _client.GetTwinAsync();
        await InitDeviceTwin(twin,stoppingToken);
        //Lastly, send some messages to simulate telemetry, Initially from Settings, later set by DesiredProperties   
        await SendDeviceToCloudMessagesAsync(_messageCount,_messageDelay,stoppingToken);
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
    
    // Async method to send simulated telemetry
    private async Task SendDeviceToCloudMessagesAsync(int messageCount, int messageDelay, CancellationToken ct)
    {
        int currentTemperature = 0;
        try
        {
            while (!ct.IsCancellationRequested)
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
                    await _client.SendEventAsync(message,ct);
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

    private Task<MethodResponse> DirectMethodExampleAsync(MethodRequest methodRequest, object userContext)
    {
        _logger.LogInformation($"\t *** {methodRequest.Name} was called.");
        _logger.LogInformation($"\t{methodRequest.DataAsJson}\n");
        MethodResponse retValue = new MethodResponse(methodRequest.Data, 200);
        return Task.FromResult(retValue);
    }

    private async Task OnC2DMessageReceivedAsync(Message receivedMessage, object _)
    {
        _logger.LogInformation($"{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
        PrintMessage(receivedMessage);

        await _client.CompleteAsync(receivedMessage);
        _logger.LogInformation($"{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");

        receivedMessage.Dispose();
    }

    private void PrintMessage(Message receivedMessage)
    {
        string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
        var formattedMessage = new StringBuilder($"Received message: [{messageData}]\n");

        // User set application properties can be retrieved from the Message.Properties dictionary.
        foreach (KeyValuePair<string, string> prop in receivedMessage.Properties)
        {
            formattedMessage.AppendLine($"\tProperty: key={prop.Key}, value={prop.Value}");
        }
        // System properties can be accessed using their respective accessors, e.g. DeliveryCount.
        formattedMessage.AppendLine($"\tDelivery count: {receivedMessage.DeliveryCount}");

        _logger.LogInformation($"{DateTime.Now}> {formattedMessage}");
    }

}
