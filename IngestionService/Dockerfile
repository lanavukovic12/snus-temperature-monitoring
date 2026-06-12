FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TemperatureMonitoringSystem.sln ./
COPY Shared/Shared.csproj Shared/
COPY IngestionService/IngestionService.csproj IngestionService/
RUN dotnet restore IngestionService/IngestionService.csproj
COPY . .
RUN dotnet publish IngestionService/IngestionService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "IngestionService.dll"]
