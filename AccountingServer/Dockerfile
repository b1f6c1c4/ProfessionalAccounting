FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /opt/accounting
COPY . .

ENV DOTNET_EnableDiagnostics=0
CMD ["dotnet", "AccountingServer.dll"]
