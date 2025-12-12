# Mailgo

Local first email campaign tool, an ASP.NET Core API with SQLite, a Create React App web dashboard, and an optional Electron desktop app.

<p align="center">
  <img src="docs/media/mailgo-homepage.png" alt="Mailgo dashboard" width="960">
</p>

## What you get

- Import recipients from a CSV file.
- Compose a campaign with rich text formatting.
- Send a test email, then send the campaign to all recipients.
- Run locally with Docker, or run in dev mode, or package as a desktop app.
- Store data in a local SQLite database.
- Keep SMTP credentials out of config files, credentials are provided at send time.

## Fastest start with Docker

Prerequisites, Docker Desktop or Docker Engine with Docker Compose.

From the repo root.

```bash
cd infra
docker compose up --build
```

Open the web UI at http://localhost:3000.

The API is available at http://localhost:8080 and the API base path is http://localhost:8080/api.

Data is stored under the repo data folder as a SQLite file, this folder is mounted into the api container.

## Local development

Prerequisites, .NET 10 SDK, Node.js 20 LTS.

### Run the backend API

```bash
cd backend
dotnet run --project src/Mailgo.AppHost/Mailgo.AppHost.csproj
```

Default API URL is http://localhost:8080.

### Run the web dashboard

```bash
cd frontend/app
npm install
REACT_APP_API_BASE_URL=http://localhost:8080/api npm start
```

Dev server is http://localhost:3000.

If you prefer a persistent setting, create frontend app .env.local and set REACT_APP_API_BASE_URL there.

## Desktop app

The Electron desktop app bundles the web build and the backend into one installable app.

- Desktop docs, see desktop/README.md
- Desktop source, see desktop

Typical flow on Windows.

```bash
cd desktop
npm install
npm run build
```

## Configuration notes

### Ports

- API, 8080
- Web, 3000

For Docker, you can change ports in infra/docker-compose.yml.

### SQLite

For Docker, the default database path is mounted and set by ConnectionStrings__Default in infra/docker-compose.yml.

For local dev, the backend uses its own configuration defaults, see backend configuration files under backend src.

### SMTP credentials

Mailgo is designed so SMTP credentials are not committed to the repo and are not stored in container environment variables.

The UI sends SMTP credentials only when sending, and the backend handles them for the send operation.

### Encryption keys for sensitive fields

The backend exposes a public key used by the UI to encrypt sensitive values before sending them to the API.

Set the private key securely in your runtime environment for non dev deployments, do not commit private keys to git.

## Build outputs

This repo uses Microsoft.DotNet.Arcade.Sdk to place build outputs under a shared artifacts folder in the repo root.

Look under artifacts for build outputs from dotnet builds.

## CSV format

Use recipient-sample.csv as the reference format for recipient imports.

## Docs

- Backend notes, backend/README.md
- Frontend notes, frontend/app/README.md
- Infra and compose notes, infra/README.md
- Desktop packaging, desktop/README.md

## Repository layout

- backend, .NET solution and API services
- frontend, Create React App dashboard
- desktop, Electron shell and packaging scripts
- infra, Docker Compose and deployment notes
- docs, documentation and media assets
- data, local SQLite files for Docker runs, gitignored
- scripts, shared automation entry points
- recipient-sample.csv, CSV format reference

## License

See LICENSE.
