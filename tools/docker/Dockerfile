#########################################################################
# Download and build code
#########################################################################

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy as builder

ARG SOURCE=https://github.com/microsoft/kernel-memory
ARG BRANCH=main

WORKDIR /build

# Clone latest code only
RUN git clone --single-branch --branch ${BRANCH} --depth 1 ${SOURCE} source

WORKDIR /build/source/service/Service

# Build code
RUN \
    dotnet build --configuration Release --output /build/output && \
    cp ../../tools/docker/content/run.sh /build/output/ && \
    chmod ugo=r /build/output/*.dll

# Generate data used outside the image to label it
RUN \
    git rev-parse --short HEAD > /build/output/.SHORT_COMMIT_ID && \
    git rev-parse HEAD > /build/output/.LONG_COMMIT_ID && \
    /usr/bin/env date +%Y%m%d > /build/output/.SHORT_DATE && \
    /usr/bin/env date +%Y-%m-%dT%H:%M:%S > /build/output/.LONG_DATE

#########################################################################
# Package the build
#########################################################################

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
#Debian: FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy

COPY --from=builder /build/output /app/

# Non-root user that will run the service
ARG USER=km

LABEL Maintainer="Devis Lucato, https://github.com/dluc"

WORKDIR /app

RUN \
    # Create user
    #Debian: useradd --create-home --user-group $USER --shell /bin/bash && \
    adduser -D -h /app -s /bin/sh $USER && \
    # Allow user to access the build
    chown -R $USER.$USER /app

# Allow to mount files, e.g. configuration files
VOLUME ["/app/data"]

# Define executable
ENTRYPOINT ["/bin/sh", "/app/run.sh"]

# Define current user
USER $USER

ENV ASPNETCORE_HTTP_PORTS 9001
