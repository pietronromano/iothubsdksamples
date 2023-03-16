# 14-March-2023
# Phased Approach to creating an Edge Module
 - 1 : Create a worker app with a BackgroundService 
 - 2 : Add DeviceClient code to send and receive messages to/from IoT Hub
 - 3 : Containerize, test on Ubuntu
 - 4 : Convert to Edge Module: 
   - Added deployment.template.json: best to copy a previously published one from a working device
   - Use ModuleClient instead of DeviceClient

# Create the appn
dotnet new worker --name SampleEdgeModule
dotnet add package Microsoft.Azure.Devices.Client

# VS Code
dotnet run

## Configure for debug
Menu: Run-> ".NET Core Launch (console)"

## Add Docker files
### Open Command Palette (Ctrl+Shift+P) and use Docker: Add Docker Files to Workspace... command
### Debug Docker: Choose config Docker .Net Launch (was created in previous step)

# Bash variable
img="sampleedgemodule"
# Create Dockerfile, then build and tag: NOTE: Image name must be lowercase
docker build -t $img -f Dockerfile .


## Build and tag for ACR (don't forget the period)
acrimg=<your acr name>.azurecr.io/sampleedgemodule:1.0
docker image build -t $acrimg .

## Push to ACR
docker image push $acrimg

# Set the device modules in iothub
az iot edge set-modules --hub-name pnriothub1 --device-id testedgemodules --content ./deployment.template.json --login "<iot hub connection string>"


# Monitor
az iot hub monitor-events -n pnriothub1 -d testedgemodules

