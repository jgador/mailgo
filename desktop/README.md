# Desktop (Electron)

This folder contains the Mailgo desktop app built with Electron.

The desktop app bundles.
- The Create React App production build as the renderer.
- The .NET backend published output, spawned as a local process.

## How it works

- In development, Electron loads the renderer from http://localhost:3000 and the backend runs separately on http://localhost:8080.
- In a packaged build, Electron loads a local file from the bundled frontend build and it spawns the bundled backend on localhost.

The renderer gets the API base URL from either.
- REACT_APP_API_BASE_URL, for web dev.
- window.electron.apiBaseUrl, for the desktop app via the Electron preload script.

## Prerequisites

- Node.js 20 LTS
- .NET 10 SDK
- Windows 10 or 11 for the default backend publish flow, because build:backend uses PowerShell and publishes win-x64 only

## Development

Install dependencies.

```bash
cd desktop
npm install
```

Run everything in dev mode, frontend, backend, electron.

```bash
npm run dev
```

What dev does.
- Starts the frontend dev server from ../frontend/app on port 3000
- Starts the backend host from ../backend/src/Mailgo.AppHost on port 8080
- Starts Electron and points it to the dev server via ELECTRON_RENDERER_URL

## Build and package

Build everything.

```bash
cd desktop
npm run build
```

Build steps.
- build:frontend runs the CRA production build and copies it into desktop/resources/frontend
- build:backend publishes the backend into desktop/resources/backend
- build:electron compiles the Electron main process to dist and runs electron-builder to produce installers under desktop/out

### Non Windows build note

On macOS or Linux, npm run build:backend exits because the script is Windows only.

You can publish the backend manually, then run build:electron.

```bash
cd desktop
dotnet publish ../backend/src/Mailgo.AppHost/Mailgo.AppHost.csproj -c Release -o ./resources/backend
npm run build:frontend
npm run build:electron
```

If you want fully supported macOS or Linux packaging, update scripts/build-backend.js and scripts/build-backend.ps1 to publish the correct runtime for your target.

## Runtime paths and data

In a packaged build, the backend listens on.

- http://localhost:8080

The desktop shell forces the SQLite connection string to use a data folder next to the backend binaries.

- resources/backend/data/app.db

This keeps desktop data local to the installed app.

## Environment variables

- MAILGO_BACKEND_PORT, override the backend port for desktop, default 8080
- ELECTRON_RENDERER_URL, dev only override for the renderer url, default http://localhost:3000
- START_EMBEDDED_BACKEND, if true, Electron will spawn the embedded backend even in dev
- SKIP_EMBEDDED_BACKEND, if true, Electron will never spawn the embedded backend

## Troubleshooting

Backend binaries not found.
- Run npm run build:backend from desktop, or publish the backend manually into desktop/resources/backend.

Port already in use.
- Stop the process using port 8080, or set MAILGO_BACKEND_PORT and restart.

API base URL must be set.
- For web dev, set REACT_APP_API_BASE_URL.
- For desktop, ensure preload is exposing window.electron.apiBaseUrl and the backend is running.

## Files and folders

- src/main, Electron main process and preload code
- scripts, build helpers for syncing frontend and publishing backend
- resources/frontend, output folder for the CRA production build
- resources/backend, output folder for the published backend
- electron-builder.yml, packaging config
