@echo off

dotnet restore
dotnet build
dotnet run setup
