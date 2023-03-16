# 09-March-2023

# Event Hub Processor
# SOURCE
https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/eventhub/Azure.Messaging.EventHubs.Processor

# Framework: 
dotnet --version
>7.0.102

# Create the project (run from folder above)
dotnet new console -o EventHubProcessor

## Generates simplified Program with implicit usings, See:
https://aka.ms/new-console-template

## Add References to packages
cd ReceiveFromDeviceToCloud
dotnet add package Azure.Messaging.EventHubs.Processor


### This is added to .csproj file:    
>    <PackageReference Include="Azure.Messaging.EventHubs.Processor" Version="5.8.0" />

## Build
dotnet build



# On another machine, Restore dependencies
dotnet restore
dotnet build

# Run from project command line: 
dotnet run 

# Run from any terminal - in the \bin\Debug\net7.0 folder
dotnet EventHubProcessor 


