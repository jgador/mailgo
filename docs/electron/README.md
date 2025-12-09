# Mailgo Desktop (Electron)

Electron shell that ships the CRA build and ASP.NET Core backend together for an offline-friendly desktop experience.

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
  - `build-backend.js` - cross-platform `dotnet publish` into `desktop/resources/backend`.
  - `build-backend.ps1` - PowerShell alternative for Windows.
  - `sync-frontend.js` - copies `frontend/app/build` into `desktop/resources/frontend`.
- `desktop/resources/` - staging area for the CRA build and published backend (gitignored).
- `desktop/out/` - installer artifacts produced by `electron-builder` (gitignored).

## Dev workflow (desktop)
```bash
cd desktop
npm install
npm run dev
```
What happens:
- `dev:frontend`: `npm start` in `frontend/app` on `http://localhost:3000`.
- `dev:backend`: `dotnet watch` for `backend/src/Mailgo.Api` on port `8080`.
- `dev:electron`: waits for the dev server, then launches Electron pointing at it.
- The renderer pulls `apiBaseUrl` from `REACT_APP_API_BASE_URL` if set, otherwise from `window.electron.apiBaseUrl` (provided by preload).
- In dev, the embedded backend is skipped; the app expects the `dotnet watch` instance on port `8080`. Set `START_EMBEDDED_BACKEND=true` if you want Electron to launch the published backend locally.

## Build + package (desktop)
```bash
cd desktop
npm install
npm run build
```
Steps run in order:
1) `PUBLIC_URL=. npm --prefix ../frontend/app run build` (relative asset paths for file:// loads)  
2) Copy CRA build into `desktop/resources/frontend`  
3) `dotnet publish` backend into `desktop/resources/backend`  
4) `tsc` main/preload -> `dist/`, then `electron-builder` -> `out/` installers  

Backend URL inside the packaged app: `http://localhost:8080/api`.  
Data location: `AppData/Roaming/Mailgo/data` on Windows, `~/Library/Application Support/Mailgo/data` on macOS, `~/.config/Mailgo/data` on Linux.

## Configuration notes
- Override the desktop backend port by setting `MAILGO_BACKEND_PORT` before launching Electron.
- For dev, `ELECTRON_RENDERER_URL` points Electron at the CRA dev server (`http://localhost:3000` by default).
- Frontend build for desktop does not need `REACT_APP_API_BASE_URL`; it falls back to `window.electron.apiBaseUrl` in production. Keep it set for web builds.
- If you see `Backend binaries not found`, run `npm run build:backend` (from `desktop/`) to republish the API.
- Backend publish is framework-dependent; the target machine needs the .NET 10 runtime. If you need a self-contained build, update `scripts/build-backend.js` to pass a runtime identifier and `--self-contained true`.

## Publishing the web/Docker version
The Docker/Nginx flow remains unchanged (`infra/docker-compose.yml`). The Electron wrapper is additive; the same CRA build can still be deployed behind Nginx while desktop packages ship their own backend.
