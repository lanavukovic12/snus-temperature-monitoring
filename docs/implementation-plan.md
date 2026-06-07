**Phase 1 — Foundation** DONE
- Create the solution, all project folders, add them to the .sln
- Set up the `Shared/` class library with all EF Core models
- Set up PostgreSQL container and run first migration to create the tables
- Include all fields needed by the spec: `Quality` (GOOD/BAD/UNCERTAIN), `AlarmPriority` (0–3), `IsConsensus` flag on stored values
- Add `SensorRegistry`, `AlarmLog`, and consensus result models so historical queries are supported from day one

---

**Phase 2 — Sensor Simulator** DONE
- Console app that generates random temperature values on a timer
- Configurable per sensor: ID, temp range, thresholds, alarm priorities
- Sends plain HTTP POST to IngestionService (no encryption yet, add it later)
- Multiple sensors run as separate threads in the same console app
- Random send interval between 1–10 seconds per reading (per spec)
- Configurable per sensor: data quality status (`GOOD`, `BAD`, or `UNCERTAIN`)
- Local alarm detection on the sensor: check value against thresholds before sending
- Apply P3-over-P1/P2 rule: if priority 3 fires, do not also trigger priority 1 or 2
- Print every measured value with timestamp to the sensor console
- Color-coded sensor console output: yellow (P1), orange (P2), red (P3)
- Attach alarm priority to each outbound message (use `0` when no alarm is active)
- Optional per-sensor malicious mode for BFT demo: corrupt values, delayed responses, or message flooding

---

**Phase 3 — IngestionService (basic)** DONE
- POST /api/ingest endpoint that receives and validates readings
- Writes raw readings to DB via EF Core
- Returns 200 OK, nothing fancy yet
- Accept and persist `Quality`, `AlarmPriority`, and timestamp from the sensor payload
- Store priority `0` for readings where no alarm was triggered

---

**Phase 4 — Fault Tolerance** DONE
- Add `SensorWatchdog` background service
- Tracks `LastSeenAt` per sensor in `SensorRegistry` table
- Marks sensors STANDBY if silent for 10 seconds
- Promotes first available STANDBY to ACTIVE if count drops below 5
- Add 30 second block mechanism for testing
- Test by killing a sensor and watching substitution happen
- Register every sensor that has ever sent a message, keeping `LastSeenAt` for all of them
- Only sensors with ACTIVE status should send readings (simulator checks registry or receives status from server)

---

**Phase 5 — Alarm Detection** DONE
- Add `AlarmDetector` service inside IngestionService
- Checks incoming value against thresholds
- Determines alarm priority (0, 1, 2, 3)
- Writes to `AlarmLog` table
- Console color output (yellow, orange, red)
- Also accept alarm priority already computed by the sensor and persist it to DB
- Server prints alarm details (sensor ID, value, priority) in matching console colors
- Forward alarms with priority > 0 to NotificationService via POST /api/notify

---

**Phase 6 — NotificationService**
- POST /api/notify endpoint that receives alarm pings from IngestionService
- SignalR hub that pushes notifications to connected clients in real time
- Simple HTML dashboard to visualize incoming alarms
- Add historical data access: `GET /api/reports/readings` and `GET /api/reports/consensus`
- Support filtering by time range and sensor ID for defense demo / historical review

---

**Phase 7 — ConsensusService**
- Worker wakes every 60 seconds
- Reads last minute of GOOD quality readings from DB
- Implements simplified BFT algorithm in `ConsensusCalculator.cs`
- Writes result back to DB with `IsConsensus = true` flag
- Exclude `UNCERTAIN` and `BAD` readings from consensus calculation (store them, but do not include)
- Detect malicious sensors (corrupt values, extreme outliers, inconsistent reports) and set their quality to `BAD` in the registry
- Research the BFT approach and document which simplified algorithm was chosen and why

---

**Phase 8 — Security**
- Add AES encryption to sensor message payload
- Add RSA/ECDSA signing in `MessageSigner.cs`
- Add replay attack protection (messageId + timestamp check)
- Add rate limiting middleware (block if >10 msg/sec per sensor)
- Use `AspNetCoreRateLimit` as recommended in the spec
- Document security risks of using concrete network IPs instead of localhost, and list mitigations applied

---

**Phase 9 — Infrastructure**
- Write Dockerfile for each service
- Write `docker-compose.yml` wiring everything together
- Write nginx.conf for routing
- Write Kubernetes manifests
- Test full system across two computers on same network
- Route `/api/ingest` → IngestionService, `/api/notify` → NotificationService, `/api/reports` → reporting endpoints
- Bind services to concrete network IP addresses (not localhost-only) for the two-computer defense demo

---

**Phase 10 — Polish**
- README with startup instructions
- Security documentation
- Screenshots of running system
- Test the 30 second block demo for defense
- Make sure sensor IP is configurable via environment variable for the two computer demo
- Dedicated `docs/security.md`: encryption, signing, replay protection, rate limiting, and real-IP risk analysis
- Document how `GOOD` / `BAD` / `UNCERTAIN` quality statuses are assigned and used
- Document BFT algorithm choice and step-by-step malicious sensor demo for defense
- Include screenshots of sensor console colors, server alarm output, dashboard, and historical reports
