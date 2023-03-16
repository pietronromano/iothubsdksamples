# 09-March-2023

# Framework: 
dotnet --version
>7.0.102

# Create the project (run from folder above)
dotnet new console -o ReceiveMethodFromCloud

## Generates simplified Program with implicit usings, See:
https://aka.ms/new-console-template

## Add References to packages
cd ReceiveMethodFromCloud
dotnet add package Microsoft.Azure.Devices.Client


### This is added to .csproj file:    
> <PackageReference Include="Microsoft.Azure.Devices.Client" Version="1.41.3" />

## Build
dotnet build

# Configure arga Variables in launch.json
## args[0]=DeviceConnectionString
    "args": ["<device connection string>"],

# Call Method from the Portal, or from the InvokeMethodFromCloud app


# On another machine, Restore dependencies
dotnet restore
dotnet build



