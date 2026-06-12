FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TemperatureMonitoringSystem.sln ./
COPY Shared/Shared.csproj Shared/
COPY SensorSimulator/SensorSimulator.csproj SensorSimulator/
RUN dotnet restore SensorSimulator/SensorSimulator.csproj
COPY . .
RUN dotnet publish SensorSimulator/SensorSimulator.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SensorSimulator.dll"]
