# TODO: add multistage

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
#FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy

# Non-root user that will run the service
ARG user=km

LABEL maintainer = "Devis Lucato, https://github.com/dluc"

# Copy build into image (cwd: service/Service/out/docker/)
COPY . /app/

# Change working directory
WORKDIR /app

RUN \
    # Create user
    #useradd --create-home --user-group $user --shell /bin/bash && \
    adduser -D -h /app -s /bin/sh $user && \
    # Allow user to access the build
    chown -R $user.$user /app && \
    # Allow user to access the build
    chmod ugo=r /app/*.dll && \
    # Ensures the entry point is executable
    chmod ugo+x /app/run.sh && \
    # Clean up destination folder
    rm -f /app/Dockerfile /app/.dockerignore

# Allow to mount files, e.g. configuration files
VOLUME ["/app/data"]

# Define executable
ENTRYPOINT ["/bin/sh", "/app/run.sh"]

# Define current user
USER $user
