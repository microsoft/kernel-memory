# This script is used also in the Docker image

if [ -f "Microsoft.KernelMemory.ServiceAssembly.dll" ]; then
    dotnet Microsoft.KernelMemory.ServiceAssembly.dll check
else
    dotnet clean
    dotnet build -c Debug -p "SolutionName=KernelMemory"
    ASPNETCORE_ENVIRONMENT=Development dotnet run check --no-build --no-restore
fi