using System.Text;
using Microsoft.Azure.Devices;

public class ServiceClientSample
{
    private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan s_operationTimeout = TimeSpan.FromSeconds(10);

    private static ServiceClient _serviceClient;
    private readonly string _hubConnectionString;
    private readonly TransportType _transportType;
    private readonly string _deviceId;

    public ServiceClientSample(string hubConnectionString, TransportType transportType, string deviceId)
    {
        _hubConnectionString = hubConnectionString ?? throw new ArgumentNullException(nameof(hubConnectionString));
        _transportType = transportType;
        _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
    }

    public async Task RunSampleAsync(TimeSpan runningTime)
    {
        using var cts = new CancellationTokenSource(runningTime);
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
            Console.WriteLine("Sample execution cancellation requested; will exit.");
        };

        try
        {
            await InitializeServiceClientAsync();
            Task sendTask = SendC2dMessagesAsync(cts.Token);
            Task receiveTask = ReceiveMessageFeedbacksAsync(cts.Token);

            await Task.WhenAll(sendTask, receiveTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unrecoverable exception caught, user action is required, so exiting...: \n{ex}");
        }

    }

    private async Task ReceiveMessageFeedbacksAsync(CancellationToken token)
    {
        // It is important to note that receiver only gets feedback messages when the device is actively running and acting on messages.
        Console.WriteLine("Starting to listen to feedback messages");

        var feedbackReceiver = _serviceClient.GetFeedbackReceiver();

        while (!token.IsCancellationRequested)
        {
            try
            {
                FeedbackBatch feedbackMessages = await feedbackReceiver.ReceiveAsync(token);
                if (feedbackMessages != null)
                {
                    Console.WriteLine("New Feedback received:");
                    Console.WriteLine($"\tEnqueue Time: {feedbackMessages.EnqueuedTime}");
                    Console.WriteLine($"\tNumber of messages in the batch: {feedbackMessages.Records.Count()}");
                    foreach (FeedbackRecord feedbackRecord in feedbackMessages.Records)
                    {
                        Console.WriteLine($"\tDevice {feedbackRecord.DeviceId} acted on message: {feedbackRecord.OriginalMessageId} with status: {feedbackRecord.StatusCode}");
                    }

                    await feedbackReceiver.CompleteAsync(feedbackMessages, token);
                }

                await Task.Delay(s_sleepDuration, token);
            }
            //catch (Exception e) when (ExceptionHelper.IsNetwork(e))
            //{
            //    Console.WriteLine($"Transient Exception occurred; will retry: {e}");
            //
            //}
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error, will need to reinitialize the client: {e}");
                await InitializeServiceClientAsync();
                feedbackReceiver = _serviceClient.GetFeedbackReceiver();
            }
        }
    }

    private async Task SendC2dMessagesAsync(CancellationToken cancellationToken)
    {
        int messageCount = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var str = $"Hello, Cloud! - Message {++messageCount }";
            using var message = new Message(Encoding.ASCII.GetBytes(str))
            {
                // An acknowledgment is sent on delivery success or failure.
                Ack = DeliveryAcknowledgement.Full
            };

            Console.WriteLine($"Sending C2D message '{str}', #{messageCount} with Id {message.MessageId} to {_deviceId}.");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _serviceClient.SendAsync(_deviceId, message, s_operationTimeout);
                    Console.WriteLine($"Sent message {messageCount} with Id {message.MessageId} to {_deviceId}.");
                    message.Dispose();
                    break;
                }
                //catch (Exception e) when (ExceptionHelper.IsNetwork(e))
                //{
                //    Console.WriteLine($"Transient Exception occurred, will retry: {e}");
                //}
                catch (Exception e)
                {
                    Console.WriteLine($"Unexpected error, will need to reinitialize the client: {e}");
                    await InitializeServiceClientAsync();
                }
                await Task.Delay(s_sleepDuration, cancellationToken);
            }
            await Task.Delay(s_sleepDuration, cancellationToken);
        }
    }

    private async Task InitializeServiceClientAsync()
    {
        if (_serviceClient != null)
        {
            await _serviceClient.CloseAsync();
            _serviceClient.Dispose();
            //_serviceClient = null;
            Console.WriteLine("Closed and disposed the current service client instance.");
        }

        /*var options = new ServiceClientOptions
        {
            SdkAssignsMessageId = Shared.SdkAssignsMessageId.WhenUnset,
        };*/
        _serviceClient = ServiceClient.CreateFromConnectionString(_hubConnectionString, _transportType); // options);
        Console.WriteLine("Initialized a new service client instance.");
    }
}
