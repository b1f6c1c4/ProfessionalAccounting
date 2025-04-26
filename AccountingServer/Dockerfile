FROM mcr.microsoft.com/dotnet/runtime:9.0

WORKDIR /opt/accounting
COPY . .

ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "AccountingServer.dll"]
CMD []
