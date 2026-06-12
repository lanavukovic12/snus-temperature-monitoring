# Implementation Plan

## Phase 1 - Foundation: DONE

- Solution and service projects are created.
- Shared EF Core models and PostgreSQL migrations exist.
- Readings include `Quality`, `AlarmPriority`, and `IsConsensus`.
- `SensorRegistry` and `AlarmLog` support fault tolerance and historical review.

## Phase 2 - Sensor Simulator: DONE

- Sensors generate values every 1-10 seconds.
- Sensor configuration includes id, range, thresholds, quality, and malicious mode.
- Sensor-side alarm detection uses priority 0-3 and color console output.
- Optional malicious modes support corrupt values, delayed responses, and message flooding.
- Sensors poll registry state and send only when ACTIVE.

## Phase 3 - IngestionService: DONE

- Receives readings, validates payloads, persists raw data, updates registry, logs alarms, and forwards alarms.
- Secure endpoint `/api/ingest/secure` accepts encrypted and signed envelopes.

## Phase 4 - Fault Tolerance: DONE

- Watchdog keeps exactly 5 active sensors when possible.
- Sensors silent for 10 seconds are demoted.
- Standby sensors are promoted when active count drops.
- Manual 30 second blocking is available through `/api/registry/{sensorId}/block`.

## Phase 5 - Alarm Detection: DONE

- Server recalculates alarm priority from thresholds.
- Alarms are stored in `AlarmLog`.
- Server console output is color-coded by priority.
- NotificationService receives alarms in real time.

## Phase 6 - NotificationService: DONE

- `POST /api/notify` receives alarm pings.
- SignalR pushes alarms to connected dashboard clients.
- Dashboard shows live alarms and historical raw/consensus data.
- Reports support sensor and time filtering.

## Phase 7 - ConsensusService: DONE

- Worker runs every minute by default.
- Reads GOOD readings from the previous minute.
- Uses a simplified BFT median/outlier algorithm.
- Stores consensus readings with `IsConsensus = true`.
- Marks malicious outlier sensors as `BAD`.
- Algorithm is documented in `docs/bft-consensus.md`.

## Phase 8 - Security: DONE

- AES encrypts sensor payloads.
- RSA/SHA-256 signs encrypted envelopes.
- `MessageId` and `SentAt` protect against replay attacks.
- Per-sensor request counting blocks more than 10 messages per second.
- Network/IP risks and mitigations are documented in `docs/security.md`.

## Phase 9 - Infrastructure: DONE

- Dockerfiles exist for IngestionService, NotificationService, ConsensusService, and SensorSimulator.
- `docker-compose.yml` runs PostgreSQL, services, consensus worker, and nginx ingress.
- `nginx.conf` routes `/api/ingest`, `/api/registry`, `/api/notify`, `/api/reports`, and `/hubs/alarms`.
- Kubernetes manifests for Minikube are under `k8s/`.

## Phase 10 - Polish: IN PROGRESS

- README contains local, Docker, and two-computer demo instructions.
- Remaining defense polish: add screenshots of running consoles/dashboard after a full demo run.
