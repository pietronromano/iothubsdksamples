using Azure.Messaging.EventHubs.Primitives;
using Azure.Storage.Blobs; //BlobContainerClient



// CS5001 expected when compiled with -target:exe or -target:winexe
public class Program
{

//args[0]=DeviceConnectionString,args[1]=MessageCount,args[2]=MessageDelay
private static async Task Main(string[] args) 
{
    Console.WriteLine("Event Hub Processor Starting...");
   
    try
    {
        string consumerGroup = args[0];
        MyEventHubProcessor myProcessor = new MyEventHubProcessor(consumerGroup);
        await myProcessor.RunProcess();
    }
    catch (System.Exception exc)
    { 
        Console.WriteLine(exc.Message + " : " + exc.StackTrace);  
    }
        Console.WriteLine("Event Hub Processor Ended.");
}


}