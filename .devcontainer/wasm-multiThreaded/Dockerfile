# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.192.0/containers/dotnet/.devcontainer/base.Dockerfile
# For details on dotnet specific container, see: https://github.com/microsoft/vscode-dev-containers/tree/main/containers/dotnet

ARG VARIANT="8.0-noble"
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

# Set up machine requirements to build the repo and the gh CLI
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        clang \
        cmake \
        cpio \
        build-essential \
        python3 \
        curl \
        git \
        lldb \
        llvm \
        liblldb-dev \
        libunwind8 \
        libunwind8-dev \
        gettext \
        libicu-dev \
        liblttng-ust-dev \
        libssl-dev \
        libkrb5-dev \
        ninja-build

SHELL ["/bin/bash", "-c"]

# Install LTS npm and node
RUN source /usr/local/share/nvm/nvm.sh && nvm install --lts

# Install V8 Engine
RUN curl -sSL "https://netcorenativeassets.blob.core.windows.net/resource-packages/external/linux/chromium-v8/v8-linux64-rel-10.8.168.zip" -o ./v8.zip \
    && unzip ./v8.zip -d /usr/local/v8 \
    && echo $'#!/usr/bin/env bash\n\
"/usr/local/v8/d8" --snapshot_blob="/usr/local/v8/snapshot_blob.bin" "$@"\n' > /usr/local/bin/v8 \
    && chmod +x /usr/local/bin/v8

# install chromium dependencies to run debugger tests:
RUN sudo apt-get install libnss3 -y \
    && apt-get install libatk1.0-0 -y \
    && apt-get install libatk-bridge2.0-0 -y \
    && apt-get install libcups2 -y \
    && apt-get install libdrm2 -y \
    && apt-get install libxkbcommon-x11-0 -y \
    && apt-get install libxcomposite-dev -y \
    && apt-get install libxdamage1 -y \
    && apt-get install libxrandr2 -y \
    && apt-get install libgbm-dev -y \
    && apt-get install libpango-1.0-0 -y \
    && apt-get install libcairo2 -y \
    && apt-get install libasound2t64 -y

# install firefox dependencies to run debugger tests:
RUN sudo apt-get install libdbus-glib-1-2 -y \
    && apt-get install libgtk-3-0 -y \
    && apt-get install libx11-xcb-dev  -y
