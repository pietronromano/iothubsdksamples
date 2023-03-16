using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Devices.Client;
public class MethodReceiveSample
{
    private readonly DeviceClient _deviceClient;

    private class DeviceData
    {
        [JsonPropertyName("name")]
#pragma warning disable CS8618
        public string Name { get; set; }
    }

    public MethodReceiveSample(DeviceClient deviceClient)
    {
        _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
    }

    public async Task RunSampleAsync(TimeSpan sampleRunningTime)
    {
        Console.WriteLine("Press Control+C to quit the sample.");
        using var cts = new CancellationTokenSource(sampleRunningTime);
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
            Console.WriteLine("Sample execution cancellation requested; will exit.");
        };

        _deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

        // Method Call processing will be enabled when the first method handler is added.
        // Setup a callback for the 'WriteToConsole' method.
        await _deviceClient.SetMethodHandlerAsync("WriteToConsole", WriteToConsoleAsync, null, cts.Token);

        // Setup a callback for the 'GetDeviceName' method.
        await _deviceClient.SetMethodHandlerAsync(
            "GetDeviceName",
            GetDeviceNameAsync,
            new DeviceData { Name = "DeviceClientMethodSample" },
            cts.Token);

        
        // Setup a callback for the 'SetTelemetryInterval' method.
        await _deviceClient.SetMethodHandlerAsync(
            "SetTelemetryInterval",
            SetTelemetryIntervalAsync,null,
            cts.Token);

        var timer = Stopwatch.StartNew();
        Console.WriteLine($"Use the IoT hub Azure Portal to call methods GetDeviceName or WriteToConsole within this time.");

        Console.WriteLine($"Waiting up to {sampleRunningTime} for IoT Hub method calls ...");
        while (!cts.IsCancellationRequested
            && (sampleRunningTime == Timeout.InfiniteTimeSpan || timer.Elapsed < sampleRunningTime))
        {
            await Task.Delay(1000);
        }

        await _deviceClient.SetMethodHandlerAsync(
            "SetTelemetryInterval",
            null,
            null);
        // You can unsubscribe from receiving a callback for direct methods by setting a null callback handler.
        await _deviceClient.SetMethodHandlerAsync(
            "GetDeviceName",
            null,
            null);

        await _deviceClient.SetMethodHandlerAsync(
            "WriteToConsole",
            null,
            null);
    }

    private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
    {
        Console.WriteLine($"\nConnection status changed to {status}.");
        Console.WriteLine($"Connection status changed reason is {reason}.\n");
    }

    private Task<MethodResponse> SetTelemetryIntervalAsync(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\t *** {methodRequest.Name} was called.");
        Console.WriteLine($"\t{methodRequest.DataAsJson}\n");
        MethodResponse retValue = new MethodResponse(methodRequest.Data, 200);
        return Task.FromResult(retValue);
    }

    private Task<MethodResponse> WriteToConsoleAsync(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\t *** {methodRequest.Name} was called.");
        Console.WriteLine($"\t{methodRequest.DataAsJson}\n");

        return Task.FromResult(new MethodResponse(methodRequest.Data, 200));
    }

    private Task<MethodResponse> GetDeviceNameAsync(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine($"\t *** {methodRequest.Name} was called.");

        MethodResponse retValue;
        if (userContext == null)
        {
            retValue = new MethodResponse(new byte[0], 500);
        }
        else
        {
            var deviceData = (DeviceData)userContext;
            string result = JsonSerializer.Serialize(deviceData);
            retValue = new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
        }

        return Task.FromResult(retValue);
    }
}