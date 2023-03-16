# 09-March-2023

# Framework: 
dotnet --version
>7.0.102

# Create the project (run from folder above)
dotnet new console -o SendFromCloud2Device

## Generates simplified Program with implicit usings, See:
https://aka.ms/new-console-template

## Add References to packages: NOTE: The ServiceClient is in Microsoft.Azure.Devices, not Microsoft.Azure.Devices.Client
cd SendFromCloud2Device
dotnet add package Microsoft.Azure.Devices


### This is added to .csproj file:    
>     <PackageReference Include="Microsoft.Azure.Devices" Version="1.38.2" />

## Build
dotnet build

# Configure arga Variables in launch.json
##    "args": ["<iot hub connection string>",
            "telemetrySDKDevice"],

# Monitor Events from Azure IoT Explorer or Azure CLI:
az iot hub monitor-events -n pnriothub1 -d telemetrySDKDevice

# IoT Hub Route: random messages have temperature > 30, generated temperatureAlert
## Routing Query: 
$body.temperature > 30

# On another machine, Restore dependencies
dotnet restore
dotnet build

# Run from project command line: notice args[] are separated with spaces, not commas
dotnet run ""<device connection string>"" "100" "1000"

# Run from any terminal - in the \bin\Debug\net7.0 folder
dotnet SendFromDeviceToCloud "<device hub connection string>" "100" "1000"


