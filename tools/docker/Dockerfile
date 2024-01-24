FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy

# Non-root user that will run the service
ARG user=km

LABEL maintainer = dluc

# Create user
RUN useradd -m -s /bin/bash -U $user

# Copy build into image (cwd: service/Service/out/docker/)
COPY . /app/

# Allow user to access the build
RUN chown -R $user.$user /app

# Define the current folder used when starting the service
WORKDIR /app

RUN \
    # Ensures the entry point is executable
    chmod ugo+x /app/run.sh && \
    # Clean up destination folder
    rm -f /app/Dockerfile /app/.dockerignore

# Allow to mount files, e.g. configuration files
VOLUME ["/app/data"]

# Define executable
ENTRYPOINT ["/bin/bash", "/app/run.sh"]

# Define current user
USER $user
