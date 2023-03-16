
using Azure.Messaging.EventHubs.Consumer;
using System.Text;
using System.Text.Json;

// CS5001 expected when compiled with -target:exe or -target:winexe
public class Program
{

//args[0]=DeviceConnectionString,args[1]=MessageCount,args[2]=MessageDelay
private static async Task Main(string[] args) 
{
    Console.WriteLine("Read From Device2Cloud Starting...");
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
        string connectionString = args[0];
        if(String.IsNullOrEmpty(connectionString)) 
        {
            Console.WriteLine("No Event Hub Connection String Found. Exiting...");
            Environment.Exit(1);
        }
        await ReceiveMessagesFromDeviceAsync(connectionString,cts.Token);
    }
    catch (System.Exception exc)
    { 
        Console.WriteLine(exc.Message + " : " + exc.StackTrace);  
    }
        Console.WriteLine("Read From Device2Cloud Ended.");
}

// Asynchronously create a PartitionReceiver for a partition and then start
// reading any messages sent from the simulated client.
private static async Task ReceiveMessagesFromDeviceAsync(string connectionString, CancellationToken ct)
{

    // Create the consumer using the default consumer group using a direct connection to the service.
       await using var consumer = new EventHubConsumerClient(
                         EventHubConsumerClient.DefaultConsumerGroupName,connectionString);

    Console.WriteLine("Listening for messages on all partitions.");

    try
    {
         // The "ReadEventsAsync" method on the consumer is a good starting point for consuming events for prototypes
        // and samples. For real-world production scenarios, it is strongly recommended that you consider using the
        // "EventProcessorClient" from the "Azure.Messaging.EventHubs.Processor" package.
         await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(ct))
        {
            Console.WriteLine($"\nMessage received on partition {partitionEvent.Partition.PartitionId}:");

            string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
            Console.WriteLine($"\tMessage body: {data}");

            Console.WriteLine("\tApplication properties (set by device):");
            foreach (KeyValuePair<string, object> prop in partitionEvent.Data.Properties)
            {
                PrintProperties(prop);
            }

            Console.WriteLine("\tSystem properties (set by IoT hub):");
            foreach (KeyValuePair<string, object> prop in partitionEvent.Data.SystemProperties)
            {
                PrintProperties(prop);
            }
        }
    }
    catch (TaskCanceledException)
    {
        // This is expected when the token is signaled; it should not be considered an
        // error in this scenario.
    }
}

private static void PrintProperties(KeyValuePair<string, object> prop)
{
    string? propValue = prop.Value is DateTime time
        ? time.ToString("O") // using a built-in date format here that includes milliseconds
        : prop.Value.ToString();

    Console.WriteLine($"\t\t{prop.Key}: {propValue}");
}

} //End Program


   

