# 09-March-2023

# Framework: 
dotnet --version
>7.0.102

# Create the project (run from folder above)
dotnet new console -o ReceiveFromDevice2Cloud

## Generates simplified Program with implicit usings, See:
https://aka.ms/new-console-template

## Add References to packages
cd ReceiveFromDeviceToCloud
dotnet add package Azure.Messaging.EventHubs


### This is added to .csproj file:    
>  <PackageReference Include="Azure.Messaging.EventHubs" Version="5.8.0" />

## Build
dotnet build

# Configure arg Variables in launch.json
## args[0]=DeviceConnectionString,args[1]=MessageCount,args[2]=MessageDelay
    "args": ["Endpoint=sb://event hub connection string"],

# Monitor Events from Azure IoT Explorer or Azure CLI:
az iot hub monitor-events -n <youriothubname> -d telemetrySDKDevice

# On another machine, Restore dependencies
dotnet restore
dotnet build


