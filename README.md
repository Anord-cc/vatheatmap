# SimConnect HeatTracker

Track your flights, store flown routes over time, and view them in a wrapped flat map or globe view.

## What It Actually Uses

Authentication is handled by:
- Discord OAuth
- Passkeys (WebAuthn), after a user has signed in once and enrolled one

SimpelSimConnect is used for:
- reading live public pilot data from the VATSIM data feed

VATSIM API will be added again:
- Coming sson

## Features

- Discord OAuth sign-in
- Optional passkey sign-in after enrollment
- VATSIM CID linking
- Live SimConnect flight tracking
- Historical route map and density map
- Wrapped flat-map rendering for long-haul routes
- Globe view
- SimBrief route overlay
- Flight history and route statistics

## Project Structure

```text
vatsim-heatmap/
|-- backend/
|   |-- app.py
|   |-- requirements.txt
|   |-- flights.db
|   `-- .env.example
`-- frontend/
    |-- index.html
    |-- link-vatsim.html
    |-- dashboard.html
    `-- roadmap.html
```

## Setup

### 1. Create a Discord OAuth app

1. Open [Discord Developer Portal](https://discord.com/developers/applications)
2. Create an application
3. Add an OAuth redirect URI:
   `http://localhost:5000/auth/callback`
4. Copy the client ID and client secret

### 2. Configure environment variables

Create `backend/.env` from `backend/.env.example`.

Minimum local setup:

```env
DISCORD_CLIENT_ID=your_discord_client_id
DISCORD_CLIENT_SECRET=your_discord_client_secret
DISCORD_REDIRECT_URI=http://localhost:5000/auth/callback
PUBLIC_BASE_URL=http://localhost:5000
SECRET_KEY=replace_with_a_long_random_secret
ALLOWED_DISCORD_IDS=123456789012345678
```

Optional variables:

```env
SIMBRIEF_USERNAME=
SIMBRIEF_USERID=
SIMCONNECT_TELEMETRY_TOKEN=
PASSKEY_RP_ID=localhost
PASSKEY_ORIGIN=http://localhost:5000
PASSKEY_RP_NAME=VATSIM HeatTracker
VATSIM_CACHE_TTL_SECONDS=15
SIMBRIEF_CACHE_TTL_SECONDS=300
TRACKER_POLL_SECONDS=20
TRACKER_MIN_INSERT_SECONDS=10
```

Notes:
- `ALLOWED_DISCORD_IDS` is a comma-separated allowlist of Discord user IDs.
- `PASSKEY_RP_ID` and `PASSKEY_ORIGIN` should match the real site hostname and origin in production.
- Passkeys require HTTPS in production.

### 3. Install dependencies

```bash
cd backend
python -m venv venv
source venv/bin/activate
pip install -r requirements.txt
```

On Windows PowerShell:

```powershell
cd backend
python -m venv venv
.\venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

### 4. Start the backend

From the project root:

```powershell
python backend\app.py
```

The app runs on:
- [http://localhost:5000](http://localhost:5000)

## Login Flow

1. Sign in with Discord
2. If your Discord ID is allowed, your account session is created
3. Link your VATSIM CID on `/link-vatsim`
4. Optionally add a passkey for future sign-ins
5. Open `/dashboard` to view live and historical data

## How Tracking Works

1. The backend polls the VATSIM public data feed on a background interval
2. For every linked VATSIM CID currently online, it records position snapshots
3. Snapshots are written into `flight_points`
4. Flights are grouped into `flights`
5. `/api/heatmap` builds route segments, dense nodes, and recent tracks from stored points

## Main Routes

### Auth and account

- `GET /auth/login` - start Discord OAuth
- `GET /auth/callback` - Discord OAuth callback
- `GET /auth/logout` - end session
- `GET /api/me` - current logged-in user
- `POST /api/passkey/register/options` - start passkey registration
- `POST /api/passkey/register/verify` - finish passkey registration
- `POST /api/passkey/auth/options` - start passkey login
- `POST /api/passkey/auth/verify` - finish passkey login

### VATSIM linking

- `POST /api/link-vatsim` - link a VATSIM CID
- `POST /api/unlink-vatsim` - unlink the VATSIM CID

### Tracking and dashboard data

- `GET /api/live` - current live flight state for the linked CID
- `GET /api/heatmap` - stored route and density data
- `GET /api/flights` - recent flights
- `GET /api/stats` - totals and top routes
- `GET /api/simbrief` - latest SimBrief summary and route overlay
- `POST /mcp` - remote MCP endpoint for ChatGPT/API clients
- `GET /.well-known/oauth-protected-resource` - MCP OAuth resource metadata
- `GET /.well-known/oauth-authorization-server` - OAuth authorization server metadata
- `POST /oauth/register` - dynamic client registration for MCP clients
- `GET /oauth/authorize` - OAuth authorization code flow for MCP clients
- `POST /oauth/token` - OAuth token exchange for MCP clients

## Database

- `users` - Discord account identity plus linked VATSIM CID
- `passkeys` - enrolled WebAuthn credentials
- `flight_points` - recorded VATSIM position snapshots
- `flights` - tracked flight sessions

## Deployment Notes

- Use a production WSGI server instead of Flask debug serving
- Set a strong `SECRET_KEY`
- Set `DISCORD_REDIRECT_URI` to your public callback URL
- Set `PASSKEY_RP_ID` and `PASSKEY_ORIGIN` to the production domain
- Serve over HTTPS, especially for passkeys
- Keep `ALLOWED_DISCORD_IDS` restricted to the users who should access the tracker

## License Notice

All source files in this repository, including C#, HTML, CSS, JavaScript, and documentation, are licensed under the PolyForm Noncommercial License 1.0.0 unless otherwise stated.

Commercial use is prohibited without written permission from the copyright holder.
