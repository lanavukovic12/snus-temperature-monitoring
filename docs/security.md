# Security notes

Sensor messages are sent through `POST /api/ingest/secure`.

Implemented protections:

- AES encryption protects the sensor reading payload.
- RSA/SHA-256 signatures protect sender authenticity and message integrity.
- The encrypted envelope includes `SensorId`, `MessageId`, `SentAt`, IV, ciphertext, and signature.
- Replay protection rejects messages whose `MessageId` is not greater than the last accepted id for that sensor.
- Timestamp validation rejects messages outside the configured clock skew window.
- DoS protection tracks per-sensor message frequency and blocks a sensor when it sends more than 10 messages in one second.

The repository contains demo AES/RSA keys in `appsettings.json` so the project can run immediately. For a real deployment, replace them through environment variables or secrets:

- `SecureMessaging__AesKeyBase64`
- `SecureMessaging__Rsa__Modulus`
- `SecureMessaging__Rsa__Exponent`
- private RSA fields only on sensor machines

Network IP risk analysis:

Using a concrete LAN IP instead of `localhost` exposes the API to other machines on the same network. The main risks are packet capture, forged sensor messages, replay attacks, request flooding, and accidental access to internal endpoints. The project mitigates these with encrypted payloads, RSA signatures, replay checks, rate limiting, and a single ingress route. In a production deployment, add HTTPS/TLS certificates, firewall rules that allow only expected sensor subnets, and rotate keys regularly.

For the two-computer demo, run the server with Docker or `dotnet run`, then set the simulator computer to:

```powershell
$env:INGESTION_BASE_URL = "http://<server-lan-ip>:5055"
dotnet run --project SensorSimulator
```
