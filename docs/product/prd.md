# Local Email Campaign Tool – Product Requirements Document (PRD)

## 1. Overview

A small self hosted email campaign tool that runs locally in Docker.  
It lets the user:

- Upload recipients from a CSV file into a local SQLite database.
- Compose an email campaign (subject, HTML body, text fallback).
- Send that campaign to all stored recipients via an SMTP server, using credentials entered by the user at send time.
- See a simple status per campaign (Draft, Sending, Completed, Failed).

No tracking of clicks or opens in v1.

---

## 2. Goals

- Provide a simple, local only replacement for basic SendGrid style campaigns.
- Be easy to spin up with `docker compose` on a single machine.
- Require minimal setup: optional default SMTP host and port, no external database server.
- Let the user provide email address and password on demand when sending, with no persistent storage of credentials.
- Keep the tech stack aligned with the target stack:
  - Backend: ASP.NET Core on .NET 10.
  - Database: SQLite with Entity Framework Core.
  - Frontend: create react app with TypeScript (vanilla CRA).

---

## 3. Non goals for v1

- No analytics such as open rate or click tracking.
- No multi tenant support or user management.
- No scheduling to a future time. Only “send now” in v1.
- No email templates or drag and drop designers.
- No integration with external contact lists or CRMs.
- No long term storage of SMTP passwords or access tokens.

---

## 4. User stories

1. **Upload recipients**  
   - As a user, I can upload a CSV file with email addresses so that the tool stores them locally for later campaigns.

2. **Compose a campaign**  
   - As a user, I can create a campaign by setting a name, subject, from name, from email, HTML body, and plain text body.

3. **Preview a campaign**  
   - As a user, I can preview how the email looks before sending.

4. **Send test email**  
   - As a user, I can send a test email of a campaign to a single email address after entering SMTP details and credentials.

5. **Send a campaign to all recipients**  
   - As a user, I can send the campaign to all stored recipients and see its status change from Draft to Sending to Completed or Failed, after entering SMTP details and credentials for that send.

6. **Enter SMTP credentials per send**  
   - As a user, I am prompted to enter SMTP server host, port, email or username, and password every time I perform a send or test send so that my credentials are not stored.

7. **View campaign history**  
   - As a user, I can see a list of past campaigns with basic counts of how many emails were attempted and how many failed.

---

## 5. Functional requirements

### 5.1 Recipient management

- **CSV upload**
  - Accept CSV via a UI form and submit it to the API.
  - Minimum required column: `email`.
  - Optional columns:
    - `first_name`
    - `last_name`
  - Extra columns are ignored in v1.
  - Backend validates email format and skips invalid rows.
  - On completion, the API returns:
    - `totalRows`
    - `inserted`
    - `skippedInvalid`.

- **Recipient storage**
  - All recipients are stored in SQLite using EF Core.
  - Duplicate emails are not allowed. If a CSV row contains an email that already exists, it is skipped in v1.

### 5.2 Campaigns

- **Create campaign**
  - Fields:
    - `Name` (string)
    - `Subject` (string)
    - `FromName` (string)
    - `FromEmail` (string)
    - `HtmlBody` (string)
    - `TextBody` (string, optional)
  - Initial status: `Draft`.

- **Edit campaign**
  - User can edit all fields while campaign status is `Draft`.

- **Preview**
  - Frontend renders HTML body in a preview pane using the current field values.
  - No personalization merge fields in v1.

- **Send test**
  - User clicks “Send test”.
  - UI opens a dialog that asks for:
    - Test recipient email.
    - SMTP host.
    - SMTP port.
    - SMTP username or email.
    - SMTP password.
    - Encryption option (SSL or STARTTLS or None).
  - UI sends a request with campaign id and these SMTP settings in the payload.
  - API uses the provided settings only for that request, does not store them in the database.

- **Send now**
  - When the user clicks “Send now”:
    - UI opens a dialog that asks for SMTP host, port, username or email, password, and encryption. Fields can be prefilled from defaults but password must always be typed.
    - On submit, API locks the campaign and sets status to `Sending`.
    - Background worker processes recipients and sends via SMTP using the provided settings.
    - After all recipients are processed:
      - If all sends succeeded, status becomes `Completed`.
      - If any send failed, status becomes `Failed` and failures are recorded.

### 5.3 Sending pipeline

- Background worker implemented as `BackgroundService` in ASP.NET Core inside the API container.
- Uses a simple loop:
  - Fetch one campaign with `Status = Sending`.
  - For that campaign, fetch recipients that still need to be sent.
  - Send in small batches (for example 20 per loop) with a short delay between batches.
- SMTP configuration for a sending run:
  - When `send-now` is requested, the API:
    - Validates the SMTP settings in the request payload.
    - Stores them in memory for the lifetime of that send operation only (for example attached to a `CampaignSendSession` object).
    - Does not persist credentials in SQLite.
  - Background worker uses these in memory settings while sending that campaign.
- Required SMTP fields in the request payload:
  - `SmtpHost`
  - `SmtpPort`
  - `SmtpUsername`
  - `SmtpPassword`
  - `UseSsl` or `UseStartTls` flag.

---

## 6. Data model (v1)

Entity names in `Mailgo.Domain` project.

1. **Recipient**
   - `Id` (Guid)
   - `Email` (string)
   - `FirstName` (string, nullable)
   - `LastName` (string, nullable)
   - `CreatedAt` (DateTime)

2. **Campaign**
   - `Id` (Guid)
   - `Name` (string)
   - `Subject` (string)
   - `FromName` (string)
   - `FromEmail` (string)
   - `HtmlBody` (string)
   - `TextBody` (string, nullable)
   - `Status` (enum: Draft, Sending, Completed, Failed)
   - `CreatedAt` (DateTime)
   - `LastUpdatedAt` (DateTime)

3. **CampaignSendLog**
   - `Id` (Guid)
   - `CampaignId` (Guid)
   - `RecipientId` (Guid)
   - `Status` (enum: Sent, Failed)
   - `ErrorMessage` (string, nullable)
   - `SentAt` (DateTime, nullable)

SQLite considerations:

- Use a single `DbContext` with tables with simple column types compatible with SQLite.
- Store timestamps as UTC.

Credentials considerations:

- SMTP credentials are never stored in any table.
- Passwords and access tokens live only:
  - In the HTTP request for send or test send.
  - In memory for the duration of that send operation.

---

## 7. API surface (v1)

All endpoints are local only, no authentication in v1.

Base path: `/api`.

### 7.1 Recipients

- `POST /api/recipients/upload`
  - Content type: `multipart/form-data` with `file` field.
  - Response: summary object.

- `GET /api/recipients`
  - Returns paged list of recipients with `Id`, `Email`, `FirstName`, `LastName`.

### 7.2 Campaigns

- `GET /api/campaigns`
  - Returns list of campaigns with basic stats:
    - `TotalRecipients`
    - `SentCount`
    - `FailedCount`.

- `GET /api/campaigns/{id}`
  - Returns full campaign details.

- `POST /api/campaigns`
  - Creates a new campaign from payload.

- `PUT /api/campaigns/{id}`
  - Updates fields if status is `Draft`.

- `POST /api/campaigns/{id}/send-test`
  - Payload includes:
    - `testEmail`
    - `smtpHost`
    - `smtpPort`
    - `smtpUsername`
    - `smtpPassword`
    - `useSsl` or `useStartTls`.
  - Sends a single email using provided SMTP settings.

- `POST /api/campaigns/{id}/send-now`
  - Payload includes:
    - `smtpHost`
    - `smtpPort`
    - `smtpUsername`
    - `smtpPassword`
    - `useSsl` or `useStartTls`.
  - Marks campaign as `Sending`.
  - Stores SMTP settings in memory for that send session.
  - Triggers background worker to start sending.

---

## 8. Frontend requirements (CRA + TypeScript)

Built using create react app with TypeScript template.

### 8.1 Pages

1. **Dashboard**
   - Shows list of campaigns with:
     - Name
     - Status
     - Created date
     - Sent and failed counts.
   - Button “New campaign”.
   - Link to “Upload recipients”.

2. **Upload recipients page**
   - Card with:
     - File input for CSV.
     - Button “Upload”.
   - Shows upload summary returned by API.

3. **Campaign editor page**
   - Form with fields:
     - Campaign name
     - Subject
     - From name
     - From email
   - Two text areas:
     - HTML body
     - Text body
   - Right side preview of HTML body.
   - Buttons:
     - “Save draft”
     - “Send test”
     - “Send now”
   - Clicking “Send test” or “Send now” opens an SMTP dialog that collects:
     - SMTP host
     - SMTP port
     - SMTP username or email
     - SMTP password
     - Encryption option
   - Dialog submits to the respective API endpoints.

4. **Campaign detail page**
   - Read only view of campaign fields.
   - Status badge.
   - Table of send logs with:
     - Recipient email
     - Status
     - Error message (if any).

### 8.2 UI and tech notes

- Use React Router for navigation.
- Use simple CSS or CSS modules. No Tailwind or component library required for v1.
- Use `fetch` or `axios` for API calls.
- All API base URLs configured via environment variable so that Nginx or CRA proxy can route to `api` service inside Docker.
- SMTP host and port fields in the dialog can be prefilled from environment values but password is always blank.

---

## 9. Infrastructure and deployment

### 9.1 Docker compose

Services:

1. `api`
   - Built from ASP.NET Core project.
   - Environment variables:
     - `ConnectionStrings__Default` pointing to `/data/app.db`.
     - Optional defaults:
       - `Smtp__DefaultHost`
       - `Smtp__DefaultPort`
       - `Smtp__DefaultUseSsl` or `Smtp__DefaultUseStartTls`.
   - Must not contain username or password in environment for v1.
   - Volume:
     - `./data` mapped to `/data` to persist SQLite file.

2. `web`
   - Built from CRA build output.
   - Served by Node dev server or by an Nginx container that serves static files.
   - Exposes port 3000 (or 8080) to host.
   - Talks to `api` via container name.

3. No SMTP container in v1 because sending uses an external SMTP server configured by the user.

### 9.2 Run commands

- `docker compose up --build`
- Application available at `http://localhost:3000`.

---

## 10. Future extensions

Not in scope for this PRD but possible in v2:

- Click and open tracking.
- Recipient lists and segments.
- Schedule campaigns at a future time.
- Basic authentication for local multi user setups.
- Template management and variables such as `{{first_name}}`.
- Optional secure storage of SMTP credentials using a local secrets mechanism.
