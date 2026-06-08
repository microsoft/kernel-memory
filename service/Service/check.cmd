@echo off

dotnet clean
dotnet build -c Debug -p "SolutionName=KernelMemory"
cmd /C "set ASPNETCORE_ENVIRONMENT=Development && dotnet run check --no-build --no-restore"
