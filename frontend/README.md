# Mailgo Frontend

The React dashboard now lives under `frontend/`. The CRA project stays in `app/` so UI, assets, and configuration are kept away from backend code.

## Layout

- `app/` — CRA source (TypeScript, React Router, component + asset tree)
- `docker/web.Dockerfile` — multi-stage build that outputs an Nginx image
- `tests/` — placeholder for future Vitest/Cypress suites

## Local Development

```bash
cd frontend/app
npm install
REACT_APP_API_BASE_URL=http://localhost:5000/api npm start
```

Persist env vars in `frontend/app/.env.local` (keys must stay prefixed with `REACT_APP_`).

## Production Build / Docker

```bash
cd frontend
docker build -f docker/web.Dockerfile -t mailgo-web .
```

`infra/docker-compose.yml` builds this image automatically and proxies `/api` to the backend container.
