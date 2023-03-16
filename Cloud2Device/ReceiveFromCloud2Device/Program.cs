using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Text.Json;

public class Program
{

//args[0]=DeviceConnectionString,args[1]=MessageCount,args[2]=MessageDelay
private static async Task Main(string[] args) 
{
    Console.WriteLine("Receive From Cloud 2 Device Starting...");

    try
    {
        string connectionString = args[0];
        if(String.IsNullOrEmpty(connectionString)) 
        {
            Console.WriteLine("No Device Connection String Found. Exiting...");
            Environment.Exit(1);
        }
        //NOTE: ServiceClient can send in AMQP, even though the DeviceClient listens on MQTT
        TransportType transportType = TransportType.Mqtt; // TransportType.Amqp;
        TimeSpan appRunTime = TimeSpan.FromSeconds(3600);

        using var deviceClient = DeviceClient.CreateFromConnectionString(
            connectionString,transportType);
        var sample = new MessageReceiveSample(deviceClient, transportType, appRunTime);
        await sample.RunSampleAsync();
        await deviceClient.CloseAsync();
    }
    catch (System.Exception exc)
    { 
        Console.WriteLine(exc.Message + " : " + exc.StackTrace);  
    }
        Console.WriteLine("Read From Device2Cloud Ended.");
}


} //End Program
