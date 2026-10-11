# Carpool

See README.md for architecture, environment variables, and the full command reference.

## Checks before opening a pull request

Run the same steps as CI (`.github/workflows/ci.yml`):

```bash
dotnet restore Carpool.slnx --locked-mode
dotnet build Carpool.slnx --configuration Release --no-restore
dotnet test Carpool.slnx --configuration Release --no-build

cd src/frontend
npm run lint && npm run format:check && npm run type-check && npm run build
```

## Cloud sessions

`.claude/hooks/session-start.sh` installs the .NET 10 SDK and Node 24, restores backend and frontend dependencies, and creates `src/frontend/.env.local`.

`docker compose up --build` does not work in cloud sessions: the backend and frontend image builds cannot reach NuGet/npm through the session's network proxy. Run PostgreSQL in Docker and the apps natively instead:

```bash
# Docker daemon (not started automatically)
nohup dockerd > /tmp/dockerd.log 2>&1 &

# Root .env with a random local password, then PostgreSQL only
[ -f .env ] || { cp .env.example .env; sed -i "s/^POSTGRES_PASSWORD=.*/POSTGRES_PASSWORD=$(openssl rand -hex 24)/" .env; }
docker compose up -d postgres

# API on http://localhost:8080 (applies migrations in Development)
set -a; . ./.env; set +a
export ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:8080 Database__ApplyMigrations=true
export ConnectionStrings__Database="Host=localhost;Port=$POSTGRES_PORT;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"
nohup dotnet run --project src/backend/Carpool.Api --no-launch-profile > /tmp/api.log 2>&1 &

# Frontend on http://localhost:3000
(cd src/frontend && nohup npm run dev > /tmp/web.log 2>&1 &)
```

Verify with `curl localhost:8080/api/health/ready`, which should report the database as Healthy.
