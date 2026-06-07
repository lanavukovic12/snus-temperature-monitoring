# Project Specification: Distributed System for Sensor Data Collection, Processing, and Storage

## Project Overview
The objective of this project is the development of a robust distributed system for collecting, processing, and storing data obtained from sensor nodes. 

The system must provide:
1. **Real-time value monitoring**, historical data access, and event logging.
2. **Fault tolerance**.
3. **Consistency**.
4. **Reliable and secure client-server communication**.

### Application Example
An example of the system's application is temperature monitoring in a critical industrial setup, such as the core of a nuclear power plant. The system features a large number of deployed sensors across different locations, where **exactly five sensors must be active and participating in temperature monitoring at any given moment**.

---

## 1. Value Monitoring
* **Pre-simulation Configuration:** Before starting the simulation, each sensor is assigned a unique ID, a temperature generation range, a data quality status (`GOOD`, `BAD`, or `UNCERTAIN`), and threshold limits for triggering alarms. Alarms are classified into priorities 1, 2, and 3.
* **Real-time Monitoring:** Current values are monitored by printing the measured value along with its respective timestamp to the console.
* **Alarm Triggering:** Alarms are registered locally by the sensor itself when the measured value exceeds the predefined threshold. If a priority 3 alarm is activated, priority 1 and 2 alarms should not be additionally triggered.
* **Console Visualization:** Depending on the alarm priority, the current value must be printed to the console in a corresponding color:
  * **Yellow** for Priority 1
  * **Orange** for Priority 2
  * **Red** for Priority 3
* **Server Notification:** After registering an alarm, the sensor sends a message to the server.
* **Server Output:** The server prints information regarding the specific sensor where the alarm occurred and the value that triggered it, using the color that matches the alarm priority.
* **Database Logging:** For values where no alarm is activated, priority `0` is used to simplify data logging. All data must be written to the server's database.

---

## 2. Fault-Tolerant System
* **Active Node Guarantee:** The system must ensure that exactly 5 active sensors exist at any given moment.
* **Inactivity Timeout:** A sensor is considered inactive if the server does not receive a message from it within a 10-second window.
* **Testing Mechanism:** For simulation and testing purposes, it must be possible to temporarily block a sensor for a duration of 30 seconds.
* **Historical Tracking:** The server must maintain a record of the timestamp of the last received message for every sensor that has ever been a part of the system.

---

## 3. Consistency
* **Data Transmission:** Active sensors measure the temperature every 1–10 seconds (generating a random value) and send it to the server.
* **Data Persistence:** Received values are written into a PostgreSQL or SQL Server database on the server using **Entity Framework Core**.
* **Consensus Calculation:** Every minute, a consensus value is calculated based on the data collected during the previous minute and is written into the database.
* **Flagging System:** Every stored value in the database must include a flag or identifier indicating whether it represents a standard reading or a consensus value.
* **Byzantine Fault Tolerance (BFT):** Assume that certain sensors can be malicious: they may stop responding, delay responses, or transmit corrupted/incorrect data. The consensus value must be calculated using a chosen consensus algorithm. It is required to research the BFT approach and implement a simplified version of the selected algorithm.
* **Malicious Node Mitigation:** Once a sensor is determined to be malicious, its data quality status is set to `BAD`. Only data with a `GOOD` quality status can be included in the consensus value calculation.

---

## 4. Reliable and Secure Communication
* **Confidentiality and Authenticity:** All messages sent from the client to the server must be encrypted and digitally signed to ensure the confidentiality of the message content and to verify the identity of the sender.
  * *Recommendation:* AES and RSA/ECDSA.
* **Replay Attack Protection:** To protect against replay attacks, the client must attach a timestamp and a unique message ID to each transmission. The message ID must increment sequentially with every sent message.
* **DoS Protection:** The system must be resilient against Denial of Service (DoS) attacks from malicious sensors. If a single sensor ID sends more than 10 messages per second, the server must temporarily block it.
  * *Recommendation:* `AspNetCoreRateLimit`.
* **Network Binding:** Messages must not be exchanged exclusively via the `localhost` address; concrete network IP addresses must be used instead. It is necessary to analyze the security risks of this approach, apply appropriate protective measures, and document how secure communication was established.
* **Protocol:** Communication between clients and the server must utilize **HTTP/REST**.

---

## Architecture & Microservices
The system is composed of the following microservices and components:

1. **IngestionService:** The ingestion service responsible for receiving data from sensors. Its role is to ingest data rapidly and forward it downstream for further processing. It must be highly performant and scalable to handle a massive volume of incoming messages.
2. **ConsensusService:** A worker service that reads raw data from the database, computes the consensus value every minute, and writes the result into a dedicated table. This architectural separation follows the **Command Query Responsibility Segregation (CQRS)** pattern, ensuring data intensive processing is decoupled from the API, which must remain highly available to clients.
3. **NotificationService:** The service dedicated to monitoring alarms. The `IngestionService` alerts this service when an alarm is detected. The service utilizes **SignalR** to push real-time notifications to connected clients.
4. **Ingress:** The single entry point to the system that routes traffic to the appropriate backend services (e.g., routing `/api/ingest` to the ingestion service and `/api/reports` to the reporting service).

---

## Important Notes & Project Deliverables
* **Technology Stack:** The project must be developed using **ASP.NET Core**, **Docker**, and **Kubernetes (Minikube)**. Local deployment and orchestration must be supported via a `docker-compose` environment.
* **GitHub Repository Requirements:** The repository must contain:
  * Complete source code.
  * `.yaml` configuration files (Docker Compose and Kubernetes manifests).
  * Comprehensive system startup instructions.
  * Detailed documentation of the applied security measures.
  * Screenshots of the running system.
* **Grading Criteria and Deadlines:**
  * The project carries a maximum of **30 points** if it is successfully defended by **July 19th**.
  * After this deadline, the maximum attainable score is reduced to **20 points**.
  * Project defenses before the end of the semester are scheduled during regular lab sessions: **June 16/17** and **June 23/24**. Subsequent defense dates will be arranged by appointment.
* **Team Constraints:** Teams can consist of a maximum of **three students**.
* **Defense Demonstration Requirements:** During the project defense, it is mandatory to demonstrate the system's operation across **at least two separate computers** running clients and servers independently. Teams should carefully consider hardware availability and capacity when forming working groups.
