using Microsoft.Azure.Devices;
using System.Text;
using System.Text.Json;

public class Program
{

//args[0]=HubConnectionString,args[1]=DeviceId
private static async Task Main(string[] args) 
{
    Console.WriteLine("Send From Cloud 2 Device Starting...");

    try
    {
        string connectionString = args[0];
        if(String.IsNullOrEmpty(connectionString)) 
        {
            Console.WriteLine("No Device Connection String Found. Exiting...");
            Environment.Exit(1);
        }
        string deviceId = args[1];
        //NOTE: ServiceClient can send in AMQP, even though the DeviceClient listens on MQTT
        TransportType transportType = TransportType.Amqp;
        TimeSpan appRunTime = TimeSpan.FromSeconds(3600);

        var sample = new ServiceClientSample(connectionString, transportType, deviceId);
        await sample.RunSampleAsync(appRunTime);

    }
    catch (System.Exception exc)
    { 
        Console.WriteLine(exc.Message + " : " + exc.StackTrace);  
    }
        Console.WriteLine("Read From Device2Cloud Ended.");
}


} //End Program
