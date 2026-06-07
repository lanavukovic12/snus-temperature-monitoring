**Phase 1 — Foundation**
- Create the solution, all project folders, add them to the .sln
- Set up the `Shared/` class library with all EF Core models
- Set up PostgreSQL container and run first migration to create the tables

---

**Phase 2 — Sensor Simulator**
- Console app that generates random temperature values on a timer
- Configurable per sensor: ID, temp range, thresholds, alarm priorities
- Sends plain HTTP POST to IngestionService (no encryption yet, add it later)
- Multiple sensors run as separate threads in the same console app

---

**Phase 3 — IngestionService (basic)**
- POST /api/ingest endpoint that receives and validates readings
- Writes raw readings to DB via EF Core
- Returns 200 OK, nothing fancy yet

---

**Phase 4 — Fault Tolerance**
- Add `SensorWatchdog` background service
- Tracks `LastSeenAt` per sensor in `SensorRegistry` table
- Marks sensors STANDBY if silent for 10 seconds
- Promotes first available STANDBY to ACTIVE if count drops below 5
- Add 30 second block mechanism for testing
- Test by killing a sensor and watching substitution happen

---

**Phase 5 — Alarm Detection**
- Add `AlarmDetector` service inside IngestionService
- Checks incoming value against thresholds
- Determines alarm priority (0, 1, 2, 3)
- Writes to `AlarmLog` table
- Console color output (yellow, orange, red)

---

**Phase 6 — NotificationService**
- POST /api/notify endpoint that receives alarm pings from IngestionService
- SignalR hub that pushes notifications to connected clients in real time
- Simple HTML dashboard to visualize incoming alarms

---

**Phase 7 — ConsensusService**
- Worker wakes every 60 seconds
- Reads last minute of GOOD quality readings from DB
- Implements simplified BFT algorithm in `ConsensusCalculator.cs`
- Writes result back to DB with `IsConsensus = true` flag

---

**Phase 8 — Security**
- Add AES encryption to sensor message payload
- Add RSA/ECDSA signing in `MessageSigner.cs`
- Add replay attack protection (messageId + timestamp check)
- Add rate limiting middleware (block if >10 msg/sec per sensor)

---

**Phase 9 — Infrastructure**
- Write Dockerfile for each service
- Write `docker-compose.yml` wiring everything together
- Write nginx.conf for routing
- Write Kubernetes manifests
- Test full system across two computers on same network

---

**Phase 10 — Polish**
- README with startup instructions
- Security documentation
- Screenshots of running system
- Test the 30 second block demo for defense
- Make sure sensor IP is configurable via environment variable for the two computer demo