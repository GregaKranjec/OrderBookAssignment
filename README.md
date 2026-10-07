# BTC/EUR order book

ASP.NET Core API and Vue/Vite frontend displaying Bitstamp's BTC/EUR order book and a purchase quote calculated from the available asks. 
The API polls Bitstamp once per second, saves each snapshot to SQLite, and broadcasts it through SignalR. A depth chart displays the received data. 
The purchase quote is calculated from the cheapest asks first, using only the amount needed from the last price level. Bitstamp's public order-book endpoint requires no API key.

Saving a full snapshot every second means the audit database can grow quite fast. For a real deployment, this would need a retention policy to delete old data after an agreed period.

## Run with Docker

Install Docker with Docker Compose. From the repository root:

```sh
docker compose up --build -d
```

Open **http://localhost:8080**. One container serves the frontend, API and SignalR hub. No development certificate is needed.

SQLite migrations run automatically on startup. The database is stored at `/data/audit.db`.

To stop the app:

```sh
docker compose down
```

This keeps the audit database in the Docker volume and stops new snapshots from being acquired and stored.

## Run locally

Install the .NET SDK specified in `global.json` (10.0.401 or a compatible patch) and Node.js 24.12 or newer, with npm. Trust the development certificate once:

```sh
dotnet dev-certs https --trust
```

Start the API from the repository root:

```sh
dotnet run --project src/OrderBook.Api --launch-profile https
```

To start the frontend, run from `src/OrderBook.Web`:

```sh
npm ci
npm run dev
```

Open **http://localhost:5173**. Vite proxies API and SignalR traffic to **https://localhost:5001**. The local SQLite database is created automatically at `src/OrderBook.Api/data/audit.db`.

## Run tests

With the local prerequisites installed, run the API unit tests and SQLite integration test from the repository root:

```sh
dotnet test OrderBook.sln
```
_The SQLite integration test uses a separate in-memory database and does not modify the application's audit database._

Run the frontend quote tests from `src/OrderBook.Web` after installing its dependencies with `npm ci`:

```sh
npm test
```

## Endpoints and configuration

- `/hubs/order-book`: SignalR hub; the `OrderBookUpdated` event delivers saved snapshots.
- `GET /api/order-book`: read-only latest snapshot for inspection and testing. It does not make a new Bitstamp request.
- `src/OrderBook.Api/appsettings.json`: polling interval, retry delay and local database connection.
- `compose.yaml`: container port and database connection overrides. HTTPS redirection is disabled explicitly for the local HTTP container.
