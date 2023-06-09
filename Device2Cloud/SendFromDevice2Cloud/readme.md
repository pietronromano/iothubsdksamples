# 09-March-2023

# Framework: 
dotnet --version
>7.0.102

# Create the project (run from folder above)
dotnet new console -o SendFromDeviceToCloud

## Generates simplified Program with implicit usings, See:
https://aka.ms/new-console-template

## Add References to packages
cd SendFromDeviceToCloud
dotnet add package Microsoft.Azure.Devices.Client


### This is added to .csproj file:    
> <PackageReference Include="Microsoft.Azure.Devices.Client" Version="1.41.3" />

## Build
dotnet build

# Configure arga Variables in launch.json
## args[0]=DeviceConnectionString,args[1]=MessageCount,args[2]=MessageDelay
    "args": [""<device connection string>"","100","1000"],

# Monitor Events from Azure IoT Explorer or Azure CLI:
az iot hub monitor-events -n <youriothubname> -d telemetrySDKDevice

# IoT Hub Route: random messages have temperature > 30, generated temperatureAlert
## Routing Query: 
$body.temperature > 30

# On another machine, Restore dependencies
dotnet restore
dotnet build

# Run from project command line: notice args[] are separated with spaces, not commas
dotnet run "H"<device connection string>"" "100" "1000"

# Run from any terminal - in the \bin\Debug\net7.0 folder
dotnet SendFromDeviceToCloud "<device connection string>" "100" "1000"