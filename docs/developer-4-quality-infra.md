# Developer 4 - Quality, Infra & Integration Owner

## Responsabilidad

Este módulo deja lista la base de infraestructura, logging y auditoría para `events_tickets`.

## Entregables por día

### Día 1 - Repos, Docker y envs

- `Dockerfile` multi-stage para compilar y ejecutar la app ASP.NET Core.
- `docker-compose.yml` con:
  - servicio `tickets`
  - servicio `events_mongo`
  - volumen persistente `events_mongo_data`
  - red `events_network`
  - healthcheck de MongoDB
- `.env.example` con variables necesarias para ambiente local/producción.
- `Infrastructure/nginx/events_tickets.conf` como plantilla de reverse proxy.
- `.github/workflows/pipeline-dotnet.yml` para build, push a GHCR y deploy por SSH.

### Día 2-3 - Mongo logging y auditoría

Base MongoDB:

- Database: `events_observability`
- Collections:
  - `sales_logs`
  - `ticket_logs`
  - `employee_actions`
  - `system_errors`

Eventos auditados:

- quién vendió
- cuándo vendió
- qué ticket imprimió
- reimpresiones
- cancelaciones
- errores del sistema

Endpoints de auditoría:

- `POST /api/audit/sales`
- `POST /api/audit/tickets/prints`
- `POST /api/audit/tickets/reprints`
- `POST /api/audit/tickets/cancellations`
- `GET /api/audit/tickets/{ticketId}`

Cada request genera o respeta el header `X-Correlation-ID` para rastreo entre módulos.

### Día 4+ - Integración, deploy y monitoreo

Puntos listos para validar comunicación:

- Tickets hacia API: registrar venta en `POST /api/audit/sales`.
- Tickets hacia Público: registrar impresión/reimpresión.
- Tickets hacia Access: consultar trazabilidad por `GET /api/audit/tickets/{ticketId}` o directo en MongoDB.
- Monitoreo básico: `GET /health`.
- Error handling: excepciones no controladas se guardan en `system_errors`.
- Request tracing: `X-Correlation-ID` via middleware y Nginx.

## Variables de ambiente

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
MongoLogging__ConnectionString=mongodb://events_mongo:27017
MongoLogging__DatabaseName=events_observability
```

## Pruebas rápidas

Levantar local:

```bash
cp .env.example .env
docker compose up --build
```

Healthcheck:

```bash
curl http://localhost:8080/health
```

Auditoría de venta:

```bash
curl -X POST http://localhost:8080/api/audit/sales \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: demo-trace-001" \
  -d '{
    "employeeId": "emp-001",
    "employeeName": "Developer 4",
    "ticketId": "ticket-001",
    "eventId": "event-001",
    "amount": 45000,
    "paymentMethod": "cash"
  }'
```

Ver logs en MongoDB:

```bash
docker exec -it events_mongo mongosh events_observability
db.sales_logs.find().pretty()
db.ticket_logs.find().pretty()
db.employee_actions.find().pretty()
db.system_errors.find().pretty()
```
