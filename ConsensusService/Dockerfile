FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TemperatureMonitoringSystem.sln ./
COPY Shared/Shared.csproj Shared/
COPY ConsensusService/ConsensusService.csproj ConsensusService/
RUN dotnet restore ConsensusService/ConsensusService.csproj
COPY . .
RUN dotnet publish ConsensusService/ConsensusService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConsensusService.dll"]
