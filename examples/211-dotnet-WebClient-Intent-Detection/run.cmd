@echo off

dotnet restore
dotnet build
cmd /C "set ASPNETCORE_ENVIRONMENT=Development && dotnet run"
