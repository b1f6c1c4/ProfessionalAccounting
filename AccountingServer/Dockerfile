FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine

WORKDIR /opt/accounting
COPY . .

ENV COMPlus_EnableDiagnostics=0
CMD ["dotnet", "AccountingServer.dll"]
