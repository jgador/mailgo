# Mailgo Desktop (Electron)

Electron shell that ships the Create React App build and ASP.NET Core backend together for an offline-friendly desktop experience.

## Prerequisites
- Windows 10/11 for the current packaging flow (PowerShell-based backend publish), Node 20+, .NET 10 SDK.
- macOS/Linux dev is fine, but `npm run build` requires you to publish the backend manually on those platforms.

## Layout
- `desktop/package.json` - scripts for dev, build, and packaging.
- `desktop/tsconfig.json` - builds `src/main` and `src/types` to `dist/`.
- `desktop/electron-builder.yml` - packaging targets (NSIS/DMG/AppImage) and icons.
- `desktop/src/main/` - Electron entry points:
  - `main.ts` - creates the window, routes to dev server or bundled build.
  - `preload.ts` - exposes `window.electron` (`apiBaseUrl`, `appVersion`, `openExternal`).
  - `backend.ts` - spawns the published ASP.NET Core backend and manages its lifetime.
  - `config.ts` - shared constants (frontend URL, backend port, data paths).
- `desktop/src/assets/icons/icon.png` - placeholder app icon used for packaging (replace with your branded asset).
- `desktop/scripts/` - build helpers:
  - `build-backend.js` - invokes the PowerShell publish script (Windows-only) into `desktop/resources/backend`.
  - `build-backend.ps1` - Windows publish script for the backend.
  - `sync-frontend.js` - copies `frontend/app/build` into `desktop/resources/frontend`.
- `desktop/resources/` - staging area for the Create React App build and published backend (gitignored).
- `desktop/out/` - installer artifacts produced by `electron-builder` (gitignored).

## Dev workflow (desktop)
```powershell
cd desktop
npm install
npm run dev
```
What happens:
- `dev:frontend`: `npm start` in `frontend/app` on `http://localhost:3000`.
- `dev:backend`: `dotnet watch` for `backend/src/Mailgo.AppHost` on port `8080` (via `ASPNETCORE_URLS`).
- `dev:electron`: waits for the dev server, then launches Electron pointing at it.
- The renderer pulls `apiBaseUrl` from `REACT_APP_API_BASE_URL` if set, otherwise from `window.electron.apiBaseUrl` (provided by preload).
- In dev, the embedded backend is skipped; the app expects the `dotnet watch` instance on port `8080`. Set `START_EMBEDDED_BACKEND=true` if you want Electron to launch the published backend locally; set `SKIP_EMBEDDED_BACKEND=true` to force using an external API.

## Build + package (desktop)
```powershell
cd desktop
npm install
npm run build
```
Steps run in order:
1) `PUBLIC_URL=. npm --prefix ../frontend/app run build` (relative asset paths for file:// loads)  
2) Copy Create React App build into `desktop/resources/frontend`  
3) Publish backend into `desktop/resources/backend` (Windows-only PowerShell script; on macOS/Linux run `dotnet publish ../backend/src/Mailgo.AppHost/Mailgo.AppHost.csproj -c Release -o ./resources/backend`)  
4) `tsc` main/preload -> `dist/`, then `electron-builder` -> `out/` installers  

Backend URL inside the packaged app: `http://localhost:8080/api`.  
Data location (packaged): under the app’s `resources/backend/data` directory (e.g., `%LocalAppData%\Programs\Mailgo\resources\backend\data` on Windows).

## Configuration notes
- `MAILGO_BACKEND_PORT` - override the desktop backend port (default 8080).
- `ELECTRON_RENDERER_URL` - dev-only override for the renderer URL (defaults to `http://localhost:3000`).
- `REACT_APP_API_BASE_URL` - optional for dev; in production it falls back to `window.electron.apiBaseUrl`.
- `START_EMBEDDED_BACKEND` / `SKIP_EMBEDDED_BACKEND` - control whether Electron spawns the embedded backend in dev.
- If you see `Backend binaries not found`, run `npm run build:backend` (Windows) or publish manually to `desktop/resources/backend` (non-Windows). The target machine needs the .NET 10 runtime; for a self-contained publish, adjust the publish command to include a runtime identifier and `--self-contained true`.

## Publishing the web/Docker version
The Docker/Nginx flow remains unchanged (`infra/docker-compose.yml`). The Electron wrapper is additive; the same Create React App build can still be deployed behind Nginx while desktop packages ship their own backend.
