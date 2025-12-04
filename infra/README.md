# Infrastructure

Container/deployment assets live here to keep them isolated from application code.

## Docker Compose

From the repo root:

```bash
cd infra
docker compose up --build
```

Services:

- `api` — builds from `backend/` using `backend/docker/api.Dockerfile`
- `web` — builds from `frontend/` using `frontend/docker/web.Dockerfile`

SQLite data is mounted from `../data` so it survives container rebuilds. Override SMTP defaults with `SMTP_DEFAULT_*` env vars or an `.env` file next to `docker-compose.yml`.
