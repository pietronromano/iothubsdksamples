using Microsoft.Azure.Devices;
using System.Text;
using System.Text.Json;

public class Program
{

//args[0]=HubConnectionString,args[1]=DeviceId
private static async Task Main(string[] args) 
{
    Console.WriteLine("Invoke Device Method Starting...");

    try
    {
        string connectionString = args[0];
        if(String.IsNullOrEmpty(connectionString)) 
        {
            Console.WriteLine("No Hub Connection String Found. Exiting...");
            Environment.Exit(1);
        }
        string deviceId = args[1];
        // Create a ServiceClient to communicate with service-facing endpoint on your hub.
        using var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        await InvokeMethodAsync(deviceId, serviceClient);

    }
    catch (System.Exception exc)
    { 
        Console.WriteLine(exc.Message + " : " + exc.StackTrace);  
    }
        Console.WriteLine("Invoke Device Method Ended.");
}

// Invoke the direct method on the device, passing the payload.
private static async Task InvokeMethodAsync(string deviceId, ServiceClient serviceClient)
{
    var methodInvocation = new CloudToDeviceMethod("SetTelemetryInterval")
    {
        ResponseTimeout = TimeSpan.FromSeconds(30),
    };
    methodInvocation.SetPayloadJson("10");

    Console.WriteLine($"Invoking direct method for device: {deviceId}");

    // Invoke the direct method asynchronously and get the response from the simulated device.
    CloudToDeviceMethodResult response = await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);

    Console.WriteLine($"Response status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");

}


} //End Program

