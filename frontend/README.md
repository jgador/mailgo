# Frontend

This folder contains the Mailgo web dashboard built with Create React App and TypeScript.

It provides recipient import, campaign compose with formatting, and sending flows by calling the backend API.

## Prerequisites

- Node.js 20 LTS
- npm

## Quick start

From the repo root.

```bash
cd frontend/app
npm install
REACT_APP_API_BASE_URL=http://localhost:8080/api npm start
```

Open http://localhost:3000.

The backend API should be running at http://localhost:8080.

## Configuration

Create React App only exposes environment variables that start with REACT_APP_.

### API base url

Set the API base url with REACT_APP_API_BASE_URL.

Options.

- One off for a command, set it inline when running npm start
- Persistent for local dev, create a file named .env.local inside frontend/app

Example .env.local.

```
REACT_APP_API_BASE_URL=http://localhost:8080/api
```

If you change environment variables, restart npm start.

## Available scripts

From frontend/app.

Start dev server.

```bash
npm start
```

Run tests.

```bash
npm test
```

Build production bundle.

```bash
npm run build
```

## Notes

### Rich text formatting

The toolbar actions should apply formatting in the composer and the preview should match what will be sent in the email.

If you change the editor behavior, verify.

- Subject and HTML body are correctly sent to the backend
- Plain text fallback is generated or provided when applicable
- Preview matches the final HTML body that is sent

### SMTP credential encryption

The UI fetches the SMTP public key from the backend and encrypts sensitive fields before posting them to the API.

This uses the backend endpoint.

- GET /api/keys/smtp

If this call fails, sending may be blocked or fall back to an error state, depending on the current implementation.

## Folder layout

- src, application source
- public, static assets
- build, production output after npm run build
