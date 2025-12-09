# Infrastructure

Container/deployment assets live here to keep them isolated from application code.

## Docker Compose

From the repo root:

```bash
cd infra
docker compose up --build
```

Services:

- `api` - builds from `backend/` using `backend/docker/api.Dockerfile`
- `web` - builds from `frontend/` using `frontend/docker/web.Dockerfile`

Notes:
- API is exposed on host port `8080` (container port `8080`). Frontend build is pinned to `http://localhost:8080/api` via `REACT_APP_API_BASE_URL` in `docker-compose.yml`. If you change the port, update that arg and rebuild the `web` image.

SQLite data is mounted from `../data` so it survives container rebuilds. SMTP credentials/hosts are entered through the dashboard's Sender Setup flow and never reside in container environment variables.
