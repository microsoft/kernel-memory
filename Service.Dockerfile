
ARG BUILD_IMAGE_TAG="8.0-jammy"
ARG RUN_IMAGE_TAG="8.0-alpine"

FROM mcr.microsoft.com/dotnet/aspnet:$RUN_IMAGE_TAG AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:$BUILD_IMAGE_TAG AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["service/Service/Service.csproj", "service/Service/"]
RUN dotnet restore "./service/Service/./Service.csproj"
COPY ["extensions", "extensions"]
COPY ["tools", "tools"]


# COPY . .
WORKDIR "/src/service/Service"
RUN dotnet build "./Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.KernelMemory.ServiceAssembly.dll"]