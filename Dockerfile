# Usage: docker build --platform linux/amd64 --build-arg BUILD_IMAGE_TAG=9.0-noble-amd64 \
#                     --build-arg RUN_IMAGE_TAG=9.0-alpine-amd64 .
#
# Usage: docker build --platform linux/arm64 --build-arg BUILD_IMAGE_TAG=9.0-noble-arm64v8 \
#                     --build-arg RUN_IMAGE_TAG=9.0-alpine-arm64v8 .

# See https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md#full-tag-listing
ARG BUILD_IMAGE_TAG="9.0-noble"
ARG RUN_IMAGE_TAG="9.0-alpine"

#########################################################################
# .NET build
#########################################################################

FROM mcr.microsoft.com/dotnet/sdk:$BUILD_IMAGE_TAG AS build

ARG BUILD_CONFIGURATION=Release

COPY . /src/
WORKDIR "/src/service/Service"

RUN dotnet build Service.csproj -c $BUILD_CONFIGURATION -o /app/build /p:RepoRoot=/src/
RUN dotnet publish "./Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:RepoRoot=/src/

#########################################################################
# prepare final content
#########################################################################

FROM mcr.microsoft.com/dotnet/aspnet:$RUN_IMAGE_TAG AS base

# Non-root user that will run the service
ARG USER=km

WORKDIR /app

RUN \
    # Create user
    #Debian: useradd --create-home --user-group $USER --shell /bin/bash && \
    adduser --disabled-password --home /app --shell /bin/sh $USER && \
    # Allow user to access the build
    chown -R $USER:$USER /app && \
    # Install icu-libs for Microsoft.Data.SqlClient
    apk add --no-cache icu-libs

COPY --from=build --chown=km:km --chmod=0550 /app/publish .

#########################################################################
# runtime
#########################################################################

LABEL org.opencontainers.image.authors="Devis Lucato, https://github.com/dluc"

# Define current user
USER $USER

# Disable globalization invariant mode for Microsoft.Data.SqlClient
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Used by .NET and KM to load appsettings.Production.json
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:9001
ENV ASPNETCORE_HTTP_PORTS=9001

EXPOSE 9001

# Define executable
ENTRYPOINT ["dotnet", "Microsoft.KernelMemory.ServiceAssembly.dll"]
