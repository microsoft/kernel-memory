# Usage: docker buildx build .

ARG BUILD_IMAGE_TAG="8.0-jammy"
ARG RUN_IMAGE_TAG="8.0-alpine"

ARG PLATFORM=$BUILDPLATFORM
#ARG PLATFORM=$TARGETPLATFORM

#########################################################################
# .NET build
#########################################################################

# ARG BUILDPLATFORM
FROM --platform=$PLATFORM mcr.microsoft.com/dotnet/sdk:$BUILD_IMAGE_TAG AS build

ARG BUILD_CONFIGURATION=Release

COPY . /src/
WORKDIR "/src/service/Service"
RUN dotnet build Service.csproj -c $BUILD_CONFIGURATION -o /app/build /p:RepoRoot=/src/
RUN dotnet publish "./Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:RepoRoot=/src/

#########################################################################
# prepare final content
#########################################################################

ARG PLATFORM
FROM --platform=$PLATFORM mcr.microsoft.com/dotnet/aspnet:$RUN_IMAGE_TAG AS base

# Non-root user that will run the service
ARG USER=km

WORKDIR /app

RUN \
    # Create user
    #Debian: useradd --create-home --user-group $USER --shell /bin/bash && \
    adduser -D -h /app -s /bin/sh $USER && \
    # Allow user to access the build
    chown -R $USER.$USER /app

COPY --from=build --chown=km:km --chmod=0550 /app/publish .

#########################################################################
# runtime
#########################################################################

LABEL org.opencontainers.image.authors="Devis Lucato, https://github.com/dluc"
MAINTAINER Devis Lucato "https://github.com/dluc"

# Define current user
USER $USER

# Used by .NET and KM to load appsettings.Production.json
ENV ASPNETCORE_ENVIRONMENT Production
ENV ASPNETCORE_URLS http://+:9001
ENV ASPNETCORE_HTTP_PORTS 9001

EXPOSE 9001

# Define executable
ENTRYPOINT ["dotnet", "Microsoft.KernelMemory.ServiceAssembly.dll"]
