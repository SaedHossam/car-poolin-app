# Carpool

A learning project for daily commuters in Egypt, intended to grow into an application used by real people. This repository contains the development foundation only.

## Architecture

- **Backend:** .NET 10 / ASP.NET Core REST API, EF Core, PostgreSQL.
- **Frontend:** Next.js App Router, React, TypeScript, mobile-first CSS.
- **Infrastructure:** Docker Compose for development; GitHub Actions for checks. Nothing is deployed.

The backend is a modular monolith with one executable and one database context. Future modules keep their endpoints, services, entities, and EF configurations together. Separate assemblies, generic repositories, and messaging infrastructure would add complexity without providing value yet. The frontend depends only on HTTP contracts; a later React Native app can use the same API.

Browser requests go directly to the API. CORS allows explicitly configured origins. Next.js does not own backend business logic. The development status component calls the readiness endpoint, which runs an actual EF database connectivity check.

```text
src/
  backend/
    Carpool.Api/
      Infrastructure/       EF context and exception handler
      Migrations/           Empty baseline; no product tables
      Modules/
        Identity/ Users/ Vehicles/ Trips/
        RideRequests/ Matching/ Notifications/ Reviews/
      Program.cs
    Dockerfile              Development and production build targets
  frontend/
    src/app/                App Router pages and styles
    src/components/         Status component
    src/lib/                API client
    Dockerfile
tests/Carpool.Api.Tests/     HTTP integration tests
scripts/smoke.ps1            Running-stack checks
.github/workflows/ci.yml
docker-compose.yml
.env.example
Carpool.slnx
dotnet-tools.json
global.json
Directory.Build.props
```

## Prerequisites

For the full stack: Docker Desktop with Linux containers and Docker Compose v2. No cloud account or credit card is required.

For native development: .NET 10 SDK (see global.json), Node.js 24 LTS with npm, and PostgreSQL 17 (or the Compose database). PowerShell 7 is used in the examples below; equivalents for environment variables in bash are `export NAME=value`.

## Start everything

Clone the repository, then enter its directory:

```powershell
git clone https://github.com/SaedHossam/car-poolin-app.git
Set-Location car-poolin-app
```

From the repository root:

```powershell
Copy-Item .env.example .env
# Edit .env: replace POSTGRES_PASSWORD with a random local password.
docker compose up --build -d
```

Use a long hexadecimal password to avoid connection-string quoting issues. Never commit .env. The example contains placeholders, not usable secrets.

Open:
- Frontend: http://localhost:3000 — wait for “Connected: frontend → API → PostgreSQL”.
- API liveness: http://localhost:8080/api/health
- API and database readiness: http://localhost:8080/api/health/ready
- Swagger UI (Development only): http://localhost:8080/swagger
- OpenAPI document (Development only): http://localhost:8080/openapi/v1.json

The first start downloads images and dependencies. PostgreSQL is awaited before the backend starts. The backend applies committed migrations automatically **only in Development when Database__ApplyMigrations=true**. The frontend may briefly report the API unavailable while it starts; select “Check again”.

Database data persists in a named volume. Source folders are mounted for live reload; generated build files and dependencies stay in separate volumes. Frontend development uses webpack polling for reliable Windows Docker file watching.

## Environment variables

Compose loads the root .env. Native .NET does **not** load it; set the API variables in your shell. Native Next.js reads src/frontend/.env.local.

| Variable | Used by | Purpose / default |
| --- | --- | --- |
| POSTGRES_DB | Compose | Database name; carpool |
| POSTGRES_USER | Compose | Local database user; carpool |
| POSTGRES_PASSWORD | Compose | Required local password; replace the example |
| POSTGRES_PORT | Compose | Host database port; 5432 |
| API_PORT | Compose | Host API port; 8080 |
| FRONTEND_PORT | Compose | Host frontend port; 3000 |
| NEXT_PUBLIC_API_URL | Frontend | Browser-accessible API base URL; http://localhost:8080 |
| CORS_ORIGIN | Compose | Allowed browser origin; http://localhost:3000 |
| ConnectionStrings__Database | API | Required Npgsql connection string |
| ASPNETCORE_ENVIRONMENT | API | Development locally; Production for production |
| ASPNETCORE_URLS | API | Listen address; Compose uses http://+:8080 |
| Cors__AllowedOrigins__0 | API | Explicit allowed origin; add numbered entries for more |
| Database__ApplyMigrations | API | Default false; Compose enables it in Development only |
| AllowedHosts | API | Set approved API hostnames in a future production environment |

If you change API_PORT, update NEXT_PUBLIC_API_URL. If you change FRONTEND_PORT, update CORS_ORIGIN. Docker service names such as postgres are internal; the browser must use localhost or a reachable host.

NEXT_PUBLIC variables are public and embedded at frontend build time. Never put secrets in them. Production frontend images need NEXT_PUBLIC_API_URL supplied as a build argument.

If port 5432 is already in use, set POSTGRES_PORT=5433 in .env and use Port=5433 when running the backend natively. The container-to-container connection continues to use port 5432.

Changing the password in .env does not change a database already initialized in the persistent volume. Change that database role password separately, or reset the volume only if the local data is disposable.

## Run the backend separately

Start just PostgreSQL:

```powershell
docker compose up -d postgres
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:8080"
$env:ConnectionStrings__Database = "Host=localhost;Port=5432;Database=carpool;Username=carpool;Password=<the-password-in-your-env-file>"
dotnet tool restore
dotnet restore Carpool.slnx --locked-mode
dotnet ef database update --project src/backend/Carpool.Api
dotnet watch --project src/backend/Carpool.Api run --no-launch-profile
```

Adapt the database name, user, and port if you changed them. CORS defaults to localhost:3000 in Development. A missing connection string fails startup clearly.

## Run the frontend separately

In another terminal:

```powershell
Set-Location src/frontend
Copy-Item .env.example .env.local
npm ci
npm run dev
```

Set NEXT_PUBLIC_API_URL in .env.local if the API runs elsewhere. Restart Next.js after changing environment variables.

## Tests and checks

From the root:

```powershell
dotnet restore Carpool.slnx --locked-mode
dotnet build Carpool.slnx --configuration Release --no-restore
dotnet test Carpool.slnx --configuration Release --no-build
```

The HTTP tests use an in-process API. They prove liveness works without PostgreSQL, readiness returns 503 on database failure without disclosing connection details, CORS allows the configured frontend, and production does not expose OpenAPI. They do not require a running database.

```powershell
Set-Location src/frontend
npm ci
npm run lint
npm run format:check
npm run type-check
npm run build
# To apply frontend formatting:
npm run format
```

A frontend .env.local or NEXT_PUBLIC_API_URL environment variable is required for the build. No running API is needed during build. No frontend test framework is added yet; HTTP integration tests plus lint, strict TypeScript, production build, and the running-stack smoke check cover this small foundation.

With Compose running, from the root:

```powershell
pwsh -File scripts/smoke.ps1
# Custom ports:
pwsh -File scripts/smoke.ps1 -ApiUrl http://localhost:8081 -FrontendUrl http://localhost:3001
```

CI runs backend restore/build/tests and frontend install/lint/format/type-check/build on pull requests and pushes to main. It does not deploy.

## EF Core migrations

The initial migration intentionally creates no domain tables; applying it records the baseline in __EFMigrationsHistory. No fake entity is needed merely to prove connectivity.

With ConnectionStrings__Database set in your shell:

```powershell
dotnet tool restore
dotnet ef migrations add YourChange --project src/backend/Carpool.Api --output-dir Migrations
dotnet ef database update --project src/backend/Carpool.Api
dotnet ef migrations has-pending-model-changes --project src/backend/Carpool.Api
dotnet ef migrations script --idempotent --project src/backend/Carpool.Api --output .local/migrations.sql
```

Create .local before writing scripts there. Review migrations before committing them. Production startup never applies migrations automatically; review and apply a migration script or bundle as a separate deployment operation.

## Useful Docker commands

```powershell
docker compose ps
docker compose logs -f backend frontend
docker compose exec postgres sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
docker compose stop
docker compose down
docker compose up --build -d
```

`docker compose down -v` **deletes the local database and other named volumes**. Use it only to intentionally reset disposable development data.

After backend dependency changes, rebuild/restart the backend. The frontend runs npm ci on container startup so dependency changes are picked up after restart.

The supplied production Docker targets establish a build structure, not a deployment. Production configuration must supply database credentials externally, explicit origins and hostnames, TLS at the ingress, and an appropriate database account. JSON console logging and generic Problem Details responses are configured; database error details stay out of HTTP responses. Readiness returns only component status.

## Intentionally postponed

Authentication, authorization, users, vehicles, trips, recurring-ride rules, matching, payments, maps, notifications, and reviews. Also postponed: PWA offline support/service worker, Arabic/localization, React Native, cloud infrastructure, deployment, and product UI design. No Redis, queues, microservices, or Kubernetes.

**Recommended next task:** agree on the Identity/Users model and implement a small authentication and profile slice with authorization and integration tests before adding rides.
