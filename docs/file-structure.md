SensorSystem/
│
├── SensorSystem.sln                          ← Visual Studio solution, references all projects
│
├── SensorSimulator/                          ← Console app, runs on Computer A, simulates physical sensors
│   ├── Program.cs                            ← Entry point, spins up N sensor instances, each on its own thread
│   ├── SensorConfig.cs                       ← Sensor ID, temp range, thresholds, quality status, malicious mode
│   ├── MessageSigner.cs                      ← Signs outgoing messages with RSA/ECDSA private key
│   └── EncryptionHelper.cs                   ← AES encrypts message payload before sending
│
├── IngestionService/                         ← ASP.NET Core Web API, single entry point for all sensor data
│   ├── Controllers/
│   │   └── IngestController.cs               ← POST /api/ingest, receives sensor readings
│   ├── Middleware/
│   │   ├── RateLimitMiddleware.cs            ← Blocks sensor if >10 messages/sec (DoS protection)
│   │   └── SignatureMiddleware.cs            ← Verifies RSA/ECDSA signature, decrypts AES payload
│   ├── BackgroundServices/
│   │   └── SensorWatchdog.cs                ← Runs every few seconds, maintains exactly 5 active sensors
│   ├── Services/
│   │   ├── AlarmDetector.cs                  ← Checks if reading exceeds thresholds, determines alarm priority
│   │   └── ReplayProtection.cs              ← Validates message ID is incrementing and timestamp is fresh
│   ├── appsettings.json                      ← DB connection string, server IP, port config
│   └── Dockerfile
│
├── ConsensusService/                         ← ASP.NET Core Worker Service, runs silently in background
│   ├── Worker.cs                             ← Wakes every 60s, orchestrates consensus calculation
│   ├── BftConsensus/
│   │   └── ConsensusCalculator.cs           ← BFT algorithm, excludes BAD/UNCERTAIN, flags malicious sensors
│   ├── appsettings.json
│   └── Dockerfile
│
├── NotificationService/                      ← ASP.NET Core Web API, handles real-time alarm push notifications
│   ├── Controllers/
│   │   ├── AlarmController.cs               ← POST /api/notify, receives alarm ping from IngestionService
│   │   └── ReportsController.cs             ← GET /api/reports/*, historical readings and consensus values
│   ├── Hubs/
│   │   └── AlarmHub.cs                      ← SignalR hub, pushes alarm to connected dashboard clients
│   ├── appsettings.json
│   └── Dockerfile
│
├── Shared/                                   ← Class library referenced by all backend services
│   ├── Models/
│   │   ├── SensorReading.cs                 ← Raw reading: SensorId, Value, Timestamp, AlarmPriority, Quality
│   │   ├── SensorRegistry.cs                ← Sensor roster: SensorId, Status (ACTIVE/STANDBY), LastSeenAt
│   │   ├── AlarmLog.cs                      ← Alarm record: SensorId, Value, Priority, Timestamp
│   │   └── ConsensusReading.cs              ← Consensus result: Value, Timestamp, IsConsensus flag
│   └── Data/
│       └── AppDbContext.cs                  ← EF Core DbContext, shared DB schema for all services
│
├── infra/
│   ├── nginx/
│   │   └── nginx.conf                       ← Routes /api/ingest, /api/notify, /api/reports to backend services
│   └── k8s/
│       ├── ingestion.yaml                   ← Kubernetes deployment + service for IngestionService
│       ├── consensus.yaml                   ← Kubernetes deployment for ConsensusService
│       ├── notification.yaml                ← Kubernetes deployment + service for NotificationService
│       ├── postgres.yaml                    ← Kubernetes deployment + persistent volume for PostgreSQL
│       └── ingress.yaml                     ← Kubernetes ingress rules, mirrors nginx.conf logic
│
├── docs/
│   └── security.md                           ← Encryption, signing, rate limiting, real-IP risk analysis
│
├── docker-compose.yml                        ← Spins up all services + PostgreSQL + Nginx with one command
└── README.md                                 ← Startup instructions, security documentation, screenshots