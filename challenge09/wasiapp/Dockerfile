FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022 AS build
RUN dotnet workload install wasi-experimental

WORKDIR /sdk
RUN powershell.exe Invoke-RestMethod https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-20/wasi-sdk-20.0.m-mingw.tar.gz -OutFile wasisdk.tar.gz
RUN tar -xzvf wasisdk.tar.gz *
ENV WASI_SDK_PATH=/sdk/wasi-sdk-20.0+m
ENV WASI_VERSION=20
ENV WASI_VERSION_FULL=20.0

WORKDIR /src
COPY . .

WORKDIR /src/app/
RUN dotnet publish -c Release

WORKDIR /src/host/
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-nanoserver-ltsc2022 AS final
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["/app/host"]