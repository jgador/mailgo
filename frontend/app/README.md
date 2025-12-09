# Mailgo Frontend

React + TypeScript dashboard for the Mailgo local email campaign tool (Create React App).

## Run locally
Prerequisites: Node.js 18+ and a running backend (`dotnet run --project ../backend/src/Mailgo.AppHost/Mailgo.AppHost.csproj`).

```bash
cd frontend/app
npm install
REACT_APP_API_BASE_URL=http://localhost:5004/api npm start
```

- CRA dev server is at `http://localhost:3000`.
- Persist defaults in `.env.local` (must use the `REACT_APP_` prefix).

## Environment
- `REACT_APP_API_BASE_URL` (required): API base URL, e.g. `http://localhost:5004/api` for local dev or `http://localhost:5000/api` when using Docker.
- `PUBLIC_URL` (optional): set for non-root deployments or desktop packaging.

## Build
```bash
npm run build
```
Outputs the production bundle to `build/`. Use `PUBLIC_URL=. npm run build` when building for desktop/Electron so assets resolve correctly from `file://` paths.
