# Temperature Monitoring System

Course project: a distributed temperature monitoring system with simulated sensors, a central ingestion API, alarm notifications, and a live dashboard. Data is stored in PostgreSQL.

## What runs where

| Component | Project | URL | Role |
|-----------|---------|-----|------|
| Database | Docker (`docker compose`) | `localhost:5432` | PostgreSQL |
| Ingestion API | `IngestionService` | http://localhost:5055 | Receives sensor readings, fault tolerance, alarms |
| Notifications | `NotificationService` | http://localhost:5117 | Alarm dashboard (SignalR) + historical reports |
| Sensors | `SensorSimulator` | — | Simulates 8 temperature sensors (console app) |

The simulator sends HTTP POST requests to IngestionService. When a reading crosses a threshold, IngestionService logs an alarm and forwards it to NotificationService, which pushes it to the browser dashboard in real time.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- EF Core CLI (for migrations):

```bash
dotnet tool install --global dotnet-ef
```

## First-time setup

From the repo root:

```bash
docker compose up -d
dotnet ef database update --project Shared --startup-project IngestionService
```

Database credentials are preconfigured in each service’s `appsettings.json` — no manual DB setup needed.

## Run

Open **three separate terminals** from the repo root:

```bash
# Terminal 1 — ingestion API
dotnet run --project IngestionService

# Terminal 2 — notifications + dashboard
dotnet run --project NotificationService

# Terminal 3 — sensor simulator (all 8 sensors)
dotnet run --project SensorSimulator
```

Then open **http://localhost:5117** in a browser for the live alarm dashboard.

**What you should see:** the simulator prints temperature readings (yellow/orange/red when alarms fire). IngestionService logs ingest and alarm lines. The dashboard shows live alarms and can load historical readings via the panel on the right.

### Optional: split sensors across processes

Useful for simulating multiple machines on one PC:

```powershell
$env:SENSOR_CONFIG = "machine-a"   # sensor-01 … sensor-04
dotnet run --project SensorSimulator

$env:SENSOR_CONFIG = "machine-b"   # sensor-05 … sensor-08
dotnet run --project SensorSimulator
```

On another computer, point the simulator at the machine running IngestionService:

```powershell
$env:INGESTION_BASE_URL = "http://<server-ip>:5055"
$env:SENSOR_CONFIG = "machine-a"
dotnet run --project SensorSimulator
```

## More documentation

- `docs/project_specification.md` — full requirements
- `docs/implementation-plan.md` — build phases and progress
