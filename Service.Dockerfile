
ARG BUILD_IMAGE_TAG="7.0-jammy"
ARG RUN_IMAGE_TAG="7.0-alpine"

#########################################################################
# build and publish
#########################################################################

FROM mcr.microsoft.com/dotnet/sdk:$BUILD_IMAGE_TAG AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["service/Service/Service.csproj", "service/Service/"]
RUN dotnet restore "./service/Service/./Service.csproj"

COPY ["extensions", "extensions"]
COPY ["tools", "tools"]
COPY ["service", "service"]
WORKDIR "/src/service/Service"
RUN dotnet build "./Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

#########################################################################
# run
#########################################################################

FROM mcr.microsoft.com/dotnet/aspnet:$RUN_IMAGE_TAG AS base
# Non-root user that will run the service
# ARG USER=km
# RUN \
#     # Create user
#     #Debian: useradd --create-home --user-group $USER --shell /bin/bash && \
#     adduser -D -h /app -s /bin/sh $USER && \
#     # Allow user to access the build
#     chown -R $USER.$USER /app
# # Define current user
# USER $USER

WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Define executable
ENTRYPOINT ["dotnet", "Microsoft.KernelMemory.ServiceAssembly.dll"]