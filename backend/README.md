# Mailgo Backend

ASP.NET Core + EF Core services live under `backend/`. The solution is `Mailgo.sln`, keeping API, domain model, workers, and test projects together.

## Layout

- `src/Mailgo.AppHost` — HTTP API + background services (SQLite by default)
- `src/Mailgo.AppHost/Domain` — shared entities, enums, DTOs
- `tests/` — placeholder for future xUnit/BDD specs
- `docker/` — container build assets (e.g., `api.Dockerfile`)
- `scripts/` — automation hooks (`dotnet format`, seeding, etc.)

## Local Development

```bash
cd backend
dotnet restore Mailgo.sln
dotnet run --project src/Mailgo.AppHost/Mailgo.AppHost.csproj
```

Set `ConnectionStrings__Default` to change the SQLite location (or point to another provider) and optionally override `ASPNETCORE_URLS`. SMTP information is never read from configuration—the API expects each send/test call to provide host/credential details.

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
dotnet ef migrations add <Name> --project src/Mailgo.AppHost
dotnet ef database update --project src/Mailgo.AppHost
```
