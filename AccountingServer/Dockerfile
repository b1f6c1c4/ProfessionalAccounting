FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine

WORKDIR /opt/accounting
COPY . .

ENV COMPlus_EnableDiagnostics=0 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0
CMD ["dotnet", "AccountingServer.dll"]
