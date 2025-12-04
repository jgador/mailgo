# Mailgo Backend

ASP.NET Core + EF Core services live under `backend/`. The solution is `Mailgo.sln`, keeping API, domain model, workers, and test projects together.

## Layout

- `src/Mailgo.Api` — HTTP API + background services (SQLite by default)
- `src/Mailgo.Domain` — shared entities, enums, DTOs
- `tests/` — placeholder for future xUnit/BDD specs
- `docker/` — container build assets (e.g., `api.Dockerfile`)
- `scripts/` — automation hooks (`dotnet format`, seeding, etc.)

## Local Development

```bash
cd backend
dotnet restore Mailgo.sln
dotnet run --project src/Mailgo.Api/Mailgo.Api.csproj
```

Set `ConnectionStrings__Default` and `Smtp__*` env vars to override SQLite path or SMTP defaults. The API listens on `http://localhost:5000` unless `ASPNETCORE_URLS` is set.

## Docker Build

```bash
cd backend
docker build -f docker/api.Dockerfile -t mailgo-api .
```

The Dockerfile copies the entire solution so migrations stay in sync with the running container.

## Entity Framework Migrations

Run migrations from the backend root to keep relative paths intact:

```bash
cd backend
dotnet ef migrations add <Name> --project src/Mailgo.Api
dotnet ef database update --project src/Mailgo.Api
```
