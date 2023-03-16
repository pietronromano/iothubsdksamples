
using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Text.Json;

// CS5001 expected when compiled with -target:exe or -target:winexe
public class Program
{

//args[0]=DeviceConnectionString,args[1]=MessageCount,args[2]=MessageDelay
private static async Task Main(string[] args) 
{
    Console.WriteLine("Simulated Device Starting...");
    int messageCount,messageDelay;
    // Set up a condition to quit the sample
    Console.WriteLine("Press control-C to exit.");
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (sender, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cts.Cancel();
        Console.WriteLine("Exiting...");
    };

    try
    {
        string deviceConnectionString = args[0];
        if(String.IsNullOrEmpty(deviceConnectionString)) 
        {
            Console.WriteLine("No Device Connection String Found. Exiting...");
            Environment.Exit(1);
        }
        int.TryParse(args[1], out messageCount);
        int.TryParse(args[2], out messageDelay);

        using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString);
        await SendDeviceToCloudMessagesAsync(deviceClient,messageCount,messageDelay,cts.Token);
    }
    catch (System.Exception exc)
    { 
        Console.WriteLine(exc.Message + " : " + exc.StackTrace);  
    }
        Console.WriteLine("Simulated Device Ended.");
}

// Async method to send simulated telemetry
private static async Task SendDeviceToCloudMessagesAsync(DeviceClient deviceClient, 
            int messageCount, int messageDelay, CancellationToken ct)
{
    // Initial telemetry values
    double minTemperature = 20;
    double minHumidity = 60;
    var rand = new Random();
    int messageStart = 0;
    try
    {
        while (!ct.IsCancellationRequested)
        {
            for(int i = messageStart; i < messageStart+messageCount;i++)
            {
                int messageNumber = i+1;
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                // Create JSON message
                string messageBody = JsonSerializer.Serialize(
                    new
                    {
                        messageNumber = messageNumber,
                        temperature = currentTemperature,
                        humidity = currentHumidity,
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
                    Console.WriteLine($"temperatureAlert: {currentTemperature}");

                // Send the telemetry message
                await deviceClient.SendEventAsync(message,ct);
                Console.WriteLine($"{DateTime.Now} > Sending message: {messageBody}");

                await Task.Delay(messageDelay);
            }
            messageStart+=messageCount;
        }
    }
    catch (TaskCanceledException texc)
    { 
        Console.WriteLine(texc.Message );  
    }
    catch (System.Exception exc)
    { 
        Console.WriteLine(exc.Message + " : " + exc.StackTrace);  
    }
}

} //End Program


   

