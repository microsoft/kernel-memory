FROM postgres:15.0

ARG PG_MAJOR=15
ENV PG_MAJOR=${PG_MAJOR}

RUN apt-get update && \
    apt-mark hold locales && \
    apt-get install -y --no-install-recommends \
    build-essential \
    git \
    clang \
    llvm \
    postgresql-server-dev-$PG_MAJOR \
    && apt-mark unhold locales

WORKDIR /tmp
RUN git config --global http.sslVerify false && \
    git clone --branch v0.8.0 https://github.com/pgvector/pgvector.git && \
    git config --global http.sslVerify true

WORKDIR /tmp/pgvector
RUN make
RUN make install