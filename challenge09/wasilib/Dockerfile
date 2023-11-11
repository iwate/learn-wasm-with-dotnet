FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022 AS build
RUN dotnet workload install wasi-experimental

WORKDIR /sdk
RUN powershell.exe Invoke-RestMethod https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-20/wasi-sdk-20.0.m-mingw.tar.gz -OutFile wasisdk.tar.gz
RUN tar -xzvf wasisdk.tar.gz *
RUN setx WASI_SDK_PATH "C:\sdk\wasi-sdk-20.0+m"

RUN powershell.exe Invoke-RestMethod https://aka.ms/vs/17/release/vc_redist.x64.exe -OutFile vc_redist.x64.exe
RUN vc_redist.x64.exe /install /quiet /norestart /log vc_redist.log

RUN powershell.exe Invoke-RestMethod https://www.python.org/ftp/python/3.12.0/python-3.12.0-embed-amd64.zip -OutFile python.zip
RUN mkdir python
RUN tar -xzvf python.zip -C python
RUN setx path "%path%;C:\sdk\python"

RUN powershell.exe Invoke-RestMethod https://github.com/emscripten-core/emsdk/archive/refs/tags/3.1.48.tar.gz -OutFile emsdk.tar.gz
RUN tar -xzvf .\emsdk.tar.gz *
WORKDIR /sdk/emsdk-3.1.48
COPY ./update-machine-certs.ps1 ./update-machine-certs.ps1
RUN powershell.exe .\update-machine-certs.ps1
RUN emsdk.bat install latest
RUN emsdk.bat activate latest --permanent

WORKDIR /src
COPY . .

WORKDIR /src/lib/
RUN dotnet publish -c Release --self-contained

WORKDIR /src/host/
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-nanoserver-ltsc2022 AS final
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["/app/host"]