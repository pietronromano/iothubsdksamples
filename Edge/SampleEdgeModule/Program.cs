
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

//Remove nullable warnings: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives
#nullable disable

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        //Using SDK Directly
        //services.AddHostedService<SampleSDKService>(); 
        //Using Edge
        services.AddHostedService<SampleEdgeModule>();
        
     })
    .Build();

//Run the Host -> will also call Worker.ExecuteAsync
host.Run();
