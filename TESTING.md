# Testing Guide for events_tickets

This guide explains how to test the audit system with mock data and connect to MongoDB on your VPS.

---

## Quick Start (Local Testing)

### 1. Start the application locally

```bash
cp .env.example .env
docker compose up --build
```

This starts:
- **events_tickets** on http://localhost:8080
- **events_mongo** (MongoDB) on localhost:27017

### 2. Run the test script

```bash
chmod +x scripts/test-locally.sh
./scripts/test-locally.sh
```

This will:
✅ Check health endpoint  
✅ Log 2 sales with different employees  
✅ Print a ticket  
✅ Reprint a ticket  
✅ Cancel a ticket  
✅ Get ticket history  

Each request gets a unique `X-Correlation-ID` for tracing.

### 3. Verify data in MongoDB (Local)

Connect to MongoDB directly:

```bash
docker exec -it events_mongo mongosh events_observability
```

Then run these queries:

```javascript
// Count sales
db.sales_logs.countDocuments()

// View all sales
db.sales_logs.find().pretty()

// View all ticket actions
db.ticket_logs.find().pretty()

// View all employee actions
db.employee_actions.find().pretty()

// View system errors (if any occurred)
db.system_errors.find().pretty()

// Get history of one ticket
db.ticket_logs.find({ticketId: "ticket-001"}).pretty()

// View one sale with details
db.sales_logs.findOne()
```

---

## Testing via HTTP Client (VS Code)

If you have the **REST Client** extension in VS Code:

1. Open `tests.http`
2. Click **Send Request** above each request
3. Watch responses in the right panel

Example requests are pre-configured with mock data.

---

## Connecting to MongoDB on VPS

### Prerequisites

- SSH access to your VPS (same key used in CI/CD pipeline)
- Your VPS hostname/IP

### Option 1: Query via SSH (Recommended for Security)

```bash
chmod +x scripts/query-mongo-vps.sh
./scripts/query-mongo-vps.sh your-vps-hostname root ~/.ssh/id_rsa
```

**What it does:**
- Connects via SSH
- Runs `docker exec` to access MongoDB inside the container
- Shows all collections
- Asks if you want to search a specific ticket

**Example:**

```bash
./scripts/query-mongo-vps.sh prod.example.com root ~/.ssh/id_rsa
```

### Option 2: Port Forwarding (For MongoDB GUI Tools)

If you want to use MongoDB Compass or another GUI tool:

```bash
# This forwards VPS MongoDB port to your local machine
ssh -i ~/.ssh/id_rsa -L 27017:localhost:27017 root@your-vps-hostname -N

# Keep this running in a separate terminal
```

Then connect to `mongodb://localhost:27017` with your local client.

### Option 3: SSH Directly to MongoDB

```bash
ssh -i ~/.ssh/id_rsa root@your-vps-hostname

# On the VPS:
cd /opt/quasar/events_infrastructure
docker compose exec events_mongo mongosh events_observability

# Then run MongoDB queries in the shell
db.sales_logs.find().pretty()
```

---

## What to Look For When Testing

### ✅ Sales should have:
- `EmployeeId` → who sold
- `EmployeeName` → employee name
- `TicketId` → which ticket
- `EventId` → which event
- `Amount` → price
- `PaymentMethod` → how they paid
- `CreatedAtUtc` → timestamp
- `TraceId` → correlation ID for tracing

### ✅ Ticket Logs should have:
- `EmployeeId` → who performed action
- `TicketId` → which ticket
- `Action` → "ticket_printed" / "ticket_reprinted" / "ticket_cancelled"
- `PrinterName` → (if print)
- `Reason` → why
- `TraceId` → correlation ID

### ✅ Employee Actions should have:
- `EmployeeId` → employee
- `Action` → "ticket_sold", "print_ticket", "reprint_ticket", "cancel_ticket"
- `ResourceType` → "ticket"
- `ResourceId` → ticket ID
- `Notes` → additional info
- `TraceId` → correlation ID

### ✅ Request Tracing:
- Every response includes a `traceId` header
- If you send `X-Correlation-ID` header, it's preserved and logged
- Check responses have `X-Correlation-ID` header matching what you sent

---

## Common Issues

### ❌ "Connection refused" when running locally

```bash
docker compose ps
# Check if both services are running
# If not: docker compose up --build -d
```

### ❌ "Can't connect to VPS MongoDB"

1. Verify SSH access works:
   ```bash
   ssh -i ~/.ssh/id_rsa root@your-vps-hostname "docker compose ps"
   ```

2. Check MongoDB is running on VPS:
   ```bash
   ssh -i ~/.ssh/id_rsa root@your-vps-hostname \
     "cd /opt/quasar/events_infrastructure && docker compose ps"
   ```

### ❌ Scripts not executable

```bash
chmod +x scripts/test-locally.sh
chmod +x scripts/query-mongo-vps.sh
```

---

## Test Scenarios

### Scenario 1: Complete Ticket Lifecycle

```bash
./scripts/test-locally.sh
```

Simulates:
1. Employee sells a ticket
2. Ticket is printed
3. Ticket is reprinted (lost & reissue)
4. Ticket is cancelled (refund)
5. Query full history

### Scenario 2: Manual Testing with curl

```bash
# Save correlation ID in variable
TRACE_ID="manual-test-$(date +%s)"

# Log a sale
curl -X POST http://localhost:8080/api/audit/sales \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: $TRACE_ID" \
  -d '{
    "employeeId": "emp-custom",
    "employeeName": "Your Name",
    "ticketId": "ticket-custom",
    "eventId": "event-custom",
    "amount": 99000,
    "paymentMethod": "digital_wallet"
  }'

# Get history
curl http://localhost:8080/api/audit/tickets/ticket-custom \
  -H "X-Correlation-ID: $TRACE_ID"
```

### Scenario 3: Error Handling Test

Intentionally send invalid data to test error logging:

```bash
# Missing required field
curl -X POST http://localhost:8080/api/audit/sales \
  -H "Content-Type: application/json" \
  -d '{"employeeId": "emp-001"}'
  # Should fail with 400 Bad Request
```

Then check `system_errors` collection for any captured errors.

---

## Next Steps

After testing locally:

1. **Push to feature branch** (for Day 3 code review)
2. **Deploy to VPS** (CI/CD pipeline via GitHub)
3. **Run tests on VPS** (same scripts, different MongoDB location)
4. **Check audit trail in VPS MongoDB**
5. **Integrate with other modules** (Tickets module calls these endpoints)

---

## Support

For issues:
- Check MongoDB logs: `docker logs events_mongo`
- Check app logs: `docker logs events_tickets`
- Verify network: `docker network ls` and `docker network inspect events_network`
