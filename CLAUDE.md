# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Stack

ASP.NET Core 10.0 (C#) backend with MySQL (via Dapper ORM) and MongoDB (audit logs in `events_observability` database). Docker Compose for local dev. Deployed via GitHub Actions to VPS running Docker.

## Build & Test

- **Local dev**: `docker compose up --build` (tickets service on :8080, MySQL on :3306, MongoDB on :27017)
- **Health check**: `curl http://localhost:8080/health`
- **Manual tests**: Use `tests.http` (VS Code REST Client) or `scripts/test-locally.sh`
- **No unit test framework** — verification is done via HTTP integration tests and MongoDB audit log checks
- **Tests must pass locally before committing** — use the health endpoint and test scripts to verify changes

## Code Style & Conventions

- PascalCase for classes/methods (standard C#); interfaces prefixed with `I`
- DTOs/Contracts as sealed records
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Constructor injection via DI container (configured in `Program.cs`)
- Dapper configured to map snake_case DB columns to PascalCase C# properties (`DefaultTypeMap.MatchNamesWithUnderscores = true`)
- Raw string literals for SQL queries in Dapper
- Spanish database column names throughout (e.g., `id_staff`, `nombre`, `id_cliente`)

## Important Gotchas

- **Startup sync**: `Program.cs` calls `SyncWithLaravelUsersAsync()` on boot to sync clients from Laravel users table — this may fail silently if the Laravel DB is unavailable; don't assume it succeeded
- **Email config fallback**: Supports both `Email:Host`/`Email:SmtpHost` and `Email:User`/`Email:Username` (mixing from prior refactoring) — check both patterns when modifying email settings
- **MongoDB audit DB name**: `events_observability` (not `events_logs`)
- **Request tracing**: Every endpoint should respect and generate the `X-Correlation-ID` header for request tracing in MongoDB
- **Default route**: `Auth/Login`, not `Home/Index`
- **Print server**: Separate Node.js microservice in `print-server/` subfolder (not part of .NET build)
- **Session timeout**: 8 hours with HttpOnly cookies
- **MySQL connection string**: Includes `AllowPublicKeyRetrieval=true` and `SslMode=Required` — production requires valid certs

## Git Workflow

Working on a single branch. If a different workflow is needed, the user will provide instructions.

## Database

- **MySQL**: Main data storage via Dapper ORM; schema in `Infrastructure/` folder
- **MongoDB**: Audit logs; use `docker exec -it events_mongo mongosh events_observability` to verify logs during testing
