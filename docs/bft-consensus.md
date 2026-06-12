# BFT consensus approach

The project uses a simplified Byzantine Fault Tolerant consensus strategy suitable for the defense demo.

Every consensus cycle reads GOOD quality raw readings from the previous minute. For each sensor, only the latest reading is used so that one noisy sensor cannot dominate the result by sending many values.

Algorithm steps:

1. Group readings by sensor and keep the latest value per sensor.
2. Require at least three GOOD sensors.
3. Sort the latest values and calculate the median.
4. Treat values farther than `Consensus:OutlierTolerance` from the median as Byzantine/outlier values.
5. Average the remaining accepted values and store that as a consensus reading with `IsConsensus = true`.
6. Mark outlier sensors as `BAD` in `SensorRegistry`, so future consensus cycles exclude them.

This is intentionally smaller than full PBFT: there is no multi-round voting between replicas because the project has one database-backed consensus worker. The BFT idea is preserved by tolerating malicious sensor values and making the consensus result depend only on the majority cluster of GOOD sensor reports.

Demo scenario:

1. Run the simulator with `sensor-08` configured as `CorruptValues`.
2. Wait for the `ConsensusService` cycle.
3. Open `/api/reports/consensus` from the NotificationService or dashboard.
4. Check `/api/registry`; the corrupt sensor should be marked `Bad` after it becomes an outlier.
