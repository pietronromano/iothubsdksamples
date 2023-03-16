
using System.Diagnostics;
using System.Collections.Concurrent; //ConcurrentDictionary
using Azure.Storage.Blobs; //BlobContainerClient
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor; //EventProcessorClient


public class MyEventHubProcessor {

    string storageConnectionString = "<storage connection string>";
    string blobContainerName = "eventhubprocessor";

    string eventHubsConnectionString = "Endpoint=sb://iothub-ns-<youriothubname>.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=...;EntityPath=<youriothubname>";
    string eventHubName = "<youriothubname>";

    BlobContainerClient storageClient;
    EventProcessorClient processor;

    ConcurrentDictionary<string, int> partitionEventCount = new ConcurrentDictionary<string, int>();
    CancellationTokenSource? cancellationSource;
    
    //consumerGroup = "$Default", "cg2"
    public MyEventHubProcessor(string consumerGroup) {
        this.storageClient = new BlobContainerClient(
            this.storageConnectionString,
            this.blobContainerName);

        this.processor = new EventProcessorClient(
            this.storageClient,
            consumerGroup,
            this.eventHubsConnectionString,
            this.eventHubName);
    }


   Task processEventHandler(ProcessEventArgs args)
    {
        try
        {
            if (args.CancellationToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }
            
            string partition = args.Partition.PartitionId;
            byte[] eventBody = args.Data.Body.ToArray();
            
            Debug.WriteLine($"Event from partition { partition } with length { eventBody.Length }.");
            var messageString = System.Text.Encoding.UTF8.GetString(eventBody);
             Debug.WriteLine(messageString);
        
            Console.WriteLine("Application properties (setby device):");
            foreach (var prop in args.Data.Properties)
            {
                Console.WriteLine("\t{0}: {1}", prop.Key, prop.Value);
            }
            Console.WriteLine("System properties (set by IoT Hub):");
            foreach (var prop in args.Data.SystemProperties)
            {
            Console.WriteLine("\t{0}: {1}", prop.Key, prop.Value);
            }

        }
        catch
        {
            // It is very important that you always guard against
            // exceptions in your handler code; the processor does
            // not have enough understanding of your code to
            // determine the correct action to take.  Any
            // exceptions from your handlers go uncaught by
            // the processor and will NOT be redirected to
            // the error handler.
        }

        return Task.CompletedTask;
    }
    Task processErrorHandler(ProcessErrorEventArgs args)
    {
        try
        {
            Debug.WriteLine("Error in the EventProcessorClient");
            Debug.WriteLine($"\tOperation: { args.Operation }");
            Debug.WriteLine($"\tException: { args.Exception }");
            Debug.WriteLine("");
        }
        catch
        {
            // It is very important that you always guard against
            // exceptions in your handler code; the processor does
            // not have enough understanding of your code to
            // determine the correct action to take.  Any
            // exceptions from your handlers go uncaught by
            // the processor and will NOT be handled in any
            // way.
        }

        return Task.CompletedTask;
    }

    public async Task RunProcess() {
        try
        {
            this.cancellationSource = new CancellationTokenSource();
            this.cancellationSource.CancelAfter(TimeSpan.FromSeconds(45));

            processor.ProcessEventAsync += processEventHandler;
            processor.ProcessErrorAsync += processErrorHandler;

            try
            {
                await processor.StartProcessingAsync(cancellationSource.Token);
                await Task.Delay(Timeout.Infinite, cancellationSource.Token);
            }
            catch (TaskCanceledException)
            {
                // This is expected if the cancellation token is
                // signaled.
            }
            finally
            {
                // This may take up to the length of time defined
                // as part of the configured TryTimeout of the processor;
                // by default, this is 60 seconds.

                await processor.StopProcessingAsync();
            }
        }
        catch
        {
            // The processor will automatically attempt to recover from any
            // failures, either transient or fatal, and continue processing.
            // Errors in the processor's operation will be surfaced through
            // its error handler.
            //
            // If this block is invoked, then something external to the
            // processor was the source of the exception.
        }
        finally
        {
        // It is encouraged that you unregister your handlers when you have
        // finished using the Event Processor to ensure proper cleanup.  This
        // is especially important when using lambda expressions or handlers
        // in any form that may contain closure scopes or hold other references.

        processor.ProcessEventAsync -= processEventHandler;
        processor.ProcessErrorAsync -= processErrorHandler;
        }
 
    }
}
