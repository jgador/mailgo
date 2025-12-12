# Backend

This folder contains the .NET backend for Mailgo.

It is an ASP.NET Core API that stores data in SQLite and sends email campaigns via SMTP.

## Prerequisites

- .NET 10 SDK
- Optional, Docker

## Quick start

From the repo root.

```bash
cd backend
dotnet run --project src/Mailgo.AppHost/Mailgo.AppHost.csproj
```

Default API URL is http://localhost:8080.
API base path is http://localhost:8080/api.

## Tests

Run all tests.

```bash
cd backend
dotnet test Mailgo.sln
```

Mailgo.AppHost.Tests contains integration tests for the API host.

## Configuration

Mailgo uses ASP.NET Core configuration, so values can come from appsettings, environment variables, and user secrets.

### Ports

If you need to override the URL.

```bash
ASPNETCORE_URLS=http://+:8080 dotnet run --project src/Mailgo.AppHost/Mailgo.AppHost.csproj
```

### SQLite

The default connection string is.

```
Data Source=data/app.db
```

This path is relative to the backend working directory.

Common options.

- Create a backend data folder and let it use the default path
- Override via environment variable

Example override.

```bash
ConnectionStrings__Default="Data Source=../data/mailgo.db" dotnet run --project src/Mailgo.AppHost/Mailgo.AppHost.csproj
```

### SMTP credentials

SMTP credentials are provided at send time and should not be committed to the repo.

### Encryption keys for sensitive fields

The backend exposes a public key for encrypting sensitive values in the client before they are sent to the API.

Endpoint.

- GET /api/keys/smtp

Configuration keys.

- EncryptionKeys__Smtp__KeyId
- EncryptionKeys__Smtp__PublicKeyPem
- EncryptionKeys__Smtp__PrivateKeyPem

Do not commit private keys to git.
For non dev deployments, inject the private key via environment variables, user secrets, or your secret store.

## Migrations

If you use EF Core migrations, ensure dotnet ef is installed.

```bash
dotnet tool install --global dotnet-ef
```

Then run migrations commands from the backend folder as needed.

## Docker note

infra docker compose uses the backend published output.
The docker/api.Dockerfile copies only what it needs to build and publish Mailgo.AppHost.

If you add projects or files that the Docker build should include, update docker/api.Dockerfile accordingly.

## Formatting

From the repo root.

```bash
pwsh ./format.ps1
```

## Repository layout

- src, application projects and API host
- tests, test projects
- docker, Dockerfile and container related files
- scripts, build and utility scripts
