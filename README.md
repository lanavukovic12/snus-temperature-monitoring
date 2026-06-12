# Temperature Monitoring System

Course project for supervisory control systems: a distributed temperature monitoring system with simulated sensors, ingestion API, alarm notifications, consensus calculation, and historical reports. Data is stored in PostgreSQL.

## Components

| Component | Project | URL | Role |
|-----------|---------|-----|------|
| Database | Docker | `localhost:5433` | PostgreSQL |
| Ingestion API | `IngestionService` | http://localhost:5055 | Receives secure sensor readings, fault tolerance, alarms |
| Notifications | `NotificationService` | http://localhost:5117 | SignalR alarm dashboard and historical reports |
| Consensus | `ConsensusService` | worker | Calculates minute consensus values |
| Ingress | nginx | http://localhost:8080 | Routes `/api/ingest`, `/api/reports`, `/hubs/alarms` |
| Sensors | `SensorSimulator` | console app | Simulates 8 temperature sensors |

## Prerequisites

- .NET 8 SDK
- Docker Desktop
- EF Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

## First-time local setup

```bash
docker compose up -d postgres
dotnet ef database update --project Shared --startup-project IngestionService
```

Database credentials are preconfigured in each service's `appsettings.json`. IngestionService also applies EF migrations automatically on startup for Docker runs.

## Run locally with dotnet

Open four terminals from the repository root:

```bash
dotnet run --project IngestionService
dotnet run --project NotificationService
dotnet run --project ConsensusService
dotnet run --project SensorSimulator
```

Open http://localhost:5117 for the dashboard.

Secure ingestion is enabled by default. Sensors post encrypted and RSA-signed envelopes to `/api/ingest/secure`; plain `/api/ingest` is rejected unless `SecureMessaging:Enabled=false`.

## Run server side with Docker compose

```bash
docker compose up --build
```

Open:

- http://localhost:8080 for the dashboard through ingress
- http://localhost:5055/swagger for IngestionService
- http://localhost:5117 for NotificationService directly

Run sensors locally against ingress:

```powershell
$env:INGESTION_BASE_URL = "http://localhost:8080"
dotnet run --project SensorSimulator
```

## Split sensors across two computers

On the server computer, run the services. On another computer, point the simulator to the server LAN IP:

```powershell
$env:INGESTION_BASE_URL = "http://<server-lan-ip>:5055"
$env:SENSOR_CONFIG = "machine-a"
dotnet run --project SensorSimulator
```

Use `machine-b` for the other group of sensors.

## Useful demo endpoints

- `GET /api/registry` - current sensor statuses and quality flags
- `POST /api/registry/{sensorId}/block` - block a sensor for 30 seconds
- `GET /api/reports/readings` - raw historical readings
- `GET /api/reports/consensus` - stored consensus values

## More documentation

- `docs/project_specification.md` - full requirements
- `docs/implementation-plan.md` - build phases and progress
- `docs/security.md` - encryption, signing, replay protection, rate limiting
- `docs/bft-consensus.md` - simplified BFT consensus algorithm and demo steps
