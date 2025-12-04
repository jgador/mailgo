# Mailgo – Local Email Campaign Tool

Local-first email campaign manager featuring an ASP.NET Core API (SQLite + EF Core) and a React (Vite) dashboard housed in `gemini/`.

## Prerequisites

- .NET 10 SDK
- Node 22+ & npm (for the frontend)
- Docker & Docker Compose (optional, for containerized runs)

## Running the API locally

```bash
dotnet build EmailMarketing.sln
dotnet run --project EmailMarketing.Api/EmailMarketing.Api.csproj
```

The API listens on `http://localhost:5000` by default (configure via `ConnectionStrings__Default` and the `ASPNETCORE_URLS` environment variables). SMTP defaults can be provided as `Smtp__DefaultHost`, `Smtp__DefaultPort`, etc.

## Running the frontend locally

```bash
cd gemini
npm install
REACT_APP_API_BASE_URL=http://localhost:5000/api npm start
```

`npm start` launches the CRA dev server on `http://localhost:3000`. Persist settings in `gemini/.env.local` (use the `REACT_APP_` prefixed keys like `REACT_APP_API_BASE_URL` or SMTP defaults) instead of exporting them every time.

## Docker Compose

```bash
docker compose up --build
```

- `api` (ASP.NET Core) listens on `localhost:5000`
- `web` (CRA build served via Nginx) listens on `localhost:3000` and proxies `/api` to the API container

SQLite data is persisted in `./data` on the host. Customize SMTP defaults via `SMTP_DEFAULT_*` env vars in `.env` or the shell.

## Key Features

- Recipient CSV ingestion with validation/deduplication (stored in SQLite)
- Campaign CRUD lifecycle (Draft → Sending → Completed/Failed) with live status
- SMTP test sends + production sends with per-send credentials (host, port, encryption/SNI hostname, self-signed toggle, from overrides) stored only in memory
- Background worker that batches SMTP deliveries and records per-recipient logs
- React dashboard covering dashboard stats, recipient management, campaign editing, previewing, and log inspection
