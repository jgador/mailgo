# Mailgo - Local Email Campaign Tool

Local-first email campaign manager featuring an ASP.NET Core API (SQLite + EF Core) and a React dashboard. Backend and frontend code now live in separate top-level folders so their lifecycles can evolve independently.

## Repository Layout

- `backend/` – .NET solution (`Mailgo.sln`), API + domain projects, backend Dockerfile
- `frontend/` – CRA dashboard source (`app/`), frontend Dockerfile, UI tests
- `infra/` – `docker-compose.yml` plus deployment-facing docs
- `docs/` – architecture & product docs (`docs/product/prd.md`, etc.)
- `data/` – local SQLite files mounted into containers (gitignored)
- `scripts/` – shared automation entry points (currently placeholder)
- `recipient-sample.csv` – CSV format reference for recipient imports

## Prerequisites

- .NET 10 SDK
- Node 22+ & npm (for the frontend)
- Docker & Docker Compose (optional, for containerized runs)

## Running the API locally

```bash
cd backend
dotnet build Mailgo.sln
dotnet run --project src/Mailgo.Api/Mailgo.Api.csproj
```

The API listens on `http://localhost:5000` by default (configure via `ConnectionStrings__Default` and the `ASPNETCORE_URLS` environment variables). Provide SMTP defaults with `Smtp__DefaultHost`, `Smtp__DefaultPort`, and related env vars.

## Running the frontend locally

```bash
cd frontend/app
npm install
REACT_APP_API_BASE_URL=http://localhost:5000/api npm start
```

`npm start` launches the React dev server on `http://localhost:3000`. Persist settings in `frontend/app/.env.local` (keys must stay prefixed with `REACT_APP_`) instead of exporting them every time.
Use the `Settings ▸ Sender Setup` page inside the dashboard to capture SMTP host, port, encryption, and “from” overrides—those values are stored in your browser and pre-fill the send dialogs, but passwords are still provided at send time.

## Docker Compose

```bash
cd infra
docker compose up --build
```

- `api` (ASP.NET Core) listens on `localhost:5000`
- `web` (CRA build served via Nginx) listens on `localhost:3000` and proxies `/api` to the API container

SQLite data is persisted in `../data` relative to `infra/`. Customize SMTP defaults via `SMTP_DEFAULT_*` env vars in `.env` or the shell.

## Key Features

- Recipient CSV ingestion with validation/deduplication (stored in SQLite)
- Campaign CRUD lifecycle (Draft -> Sending -> Completed/Failed) with live status
- SMTP test sends + production sends with per-send credentials (host, port, encryption/SNI hostname, self-signed toggle, from overrides) stored only in memory
- Background worker that batches SMTP deliveries and records per-recipient logs
- React dashboard covering dashboard stats, recipient management, campaign editing, previewing, and log inspection
