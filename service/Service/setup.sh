# This script can be used from the repo or from the docker image

if [ -f "Microsoft.KernelMemory.ServiceAssembly.dll" ]; then
    dotnet Microsoft.KernelMemory.ServiceAssembly.dll setup
else
    dotnet restore
    dotnet build
    dotnet run setup
fi