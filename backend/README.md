# StudentCouncil Backend

ASP.NET Core (.NET 10) Web API for the Student Council app. Clean architecture:
`Domain ← Application ← Infrastructure ← Api`. See
[`.ai/backend/backend_api_spec.md`](../.ai/backend/backend_api_spec.md) for the full specification.

All six functional areas (members/departments, tasks, documents, calendar, duties, budget) are implemented,
plus auth, notifications, background jobs, and the Phase 6 hardening (audit, health, security headers,
observability, Azure Blob storage, and CI/CD) summarised at the bottom.

## Prerequisites

- .NET 10 SDK, `dotnet ef` tools (`dotnet tool install --global dotnet-ef`)
- PostgreSQL 16+ running locally, with a `studentcouncil` database:
  ```bash
  createdb studentcouncil
  ```
- Docker (optional, for the container image)

## Configuration (local secrets)

Secrets live in User Secrets (dev) or environment / Key Vault (prod) — never in the repo or image.
From `src/StudentCouncil.Api`:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=studentcouncil;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:SigningKey" "<a 32+ char / 256-bit secret>"
dotnet user-secrets set "Seed:AdminEmail" "president@ue-varna.bg"
```

Non-secret defaults (issuer, token lifetimes, identity lockout, rate limits) are in `appsettings.json`.
Other notable keys: `Storage:Provider` (`Local` or `AzureBlob`), `Storage:ConnectionString` (Azure Blob),
and the standard `OTEL_EXPORTER_OTLP_ENDPOINT` env var (enables OpenTelemetry export; unset = no-op).

## Database migrations

```bash
# from backend/
dotnet ef migrations add <Name> --project src/StudentCouncil.Infrastructure --startup-project src/StudentCouncil.Api --output-dir Persistence/Migrations
dotnet ef database update    --project src/StudentCouncil.Infrastructure --startup-project src/StudentCouncil.Api
```

In Development/Staging the app also migrates + seeds on startup. In production migrations run from the
deployment pipeline via a self-contained bundle (see CI/CD).

## Run

```bash
# from backend/
dotnet run --project src/StudentCouncil.Api
curl http://localhost:<port>/health/live        # liveness
curl http://localhost:<port>/health             # readiness (DB + Blob + Push), per-check JSON
# Swagger (Development/Staging): http://localhost:<port>/swagger
```

On first run the seeder creates the 4 departments and an initial `OrgPresident`
(`MustChangePassword = true`); the temporary password is logged **only in Development**.

## Tests

```bash
# from backend/
dotnet test
```

- **Unit** — handlers, validators, authorization helpers, status transitions, audit recorder, storage.
- **Integration** — boots the real app against a throwaway local Postgres database created and dropped per
  test class, so **no Docker is required**. Includes the audit trail, health, security headers, and the full
  **authorization matrix** (every policy-protected endpoint × every role). In CI a Postgres service container
  backs the same connection.

## Docker

```bash
# Build context is the backend/ directory.
docker build -f backend/Dockerfile -t studentcouncil-api backend

docker run --rm -p 8080:8080 \
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5432;Database=studentcouncil;Username=postgres;Password=postgres" \
  -e Jwt__SigningKey="a-signing-key-at-least-32-bytes-long!!" \
  -e Seed__AdminEmail="president@ue-varna.bg" \
  studentcouncil-api

curl -s http://localhost:8080/health/live
```

Multi-stage build, non-root user, `HEALTHCHECK` against `/health/live`. Production does **not** auto-migrate.

## CI/CD

- **[`.github/workflows/ci.yml`](../.github/workflows/ci.yml)** — push/PR: `restore → build (warnings as
  errors) → test (Postgres service container) → publish`; on `main`, builds and pushes the image to GHCR.
- **[`.github/workflows/cd.yml`](../.github/workflows/cd.yml)** — release: builds an EF Core **migration
  bundle** and applies it (`PROD_DB_CONNECTION` secret) **before** the deploy step.

```bash
# Apply migrations manually with the same bundle the pipeline uses:
cd backend
dotnet ef migrations bundle --project src/StudentCouncil.Infrastructure --startup-project src/StudentCouncil.Api -o ./efbundle
./efbundle --connection "<prod-connection-string>"
```

## Phase 6 — hardening

- **Audit trail** — sensitive actions (member create/role-change/deactivate/reactivate, task & document
  delete, duty & expense CRUD) write an `AuditLog` row atomically with the action, via the `IAuditRecorder`
  seam.
- **Health** — split liveness (`/health/live`) and readiness (`/health`: DB + Blob + Push).
- **Security headers** — `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` on every response;
  CSP on server-rendered HTML; HSTS outside Development.
- **Storage** — `Local` and `AzureBlob` providers behind `Storage:Provider`; short-lived SAS URLs; weekly
  `OrphanFileCleanupJob` removes blobs with no database reference.
- **Observability** — OpenTelemetry metrics + traces, exported only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.

## Notes / deviations from the spec

- **.NET 10** (not 8) and **PostgreSQL** (not SQL Server) — see `.ai/backend/phase-1-foundation-plan.md` §1.
- Authorization uses the `role` claim sourced from `ApplicationUser.Role`; the AspNet roles tables exist but
  are unused.
- Snake_case naming convention for domain tables (Identity tables keep their canonical names).
- Integration tests run against a local/CI Postgres rather than Testcontainers (Phase 6 plan, decision #12).
