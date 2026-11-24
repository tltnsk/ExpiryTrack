# ExpiryTrack

ExpiryTrack is a web application for tracking items that expire (licenses, contracts, certificates, subscriptions) and managing their renewal through a role-based approval workflow.

- **Backend:** ASP.NET Core Web API (.NET 10), Entity Framework Core, MySQL
- **Frontend:** Vue 3 + TypeScript (Vite)

## Requirements

- .NET 10 SDK
- Node.js 22.18+ and npm
- MySQL Server 8.0 or 8.4
- EF Core command-line tool:

```bash
dotnet tool install --global dotnet-ef --version 9.*
```

## 1. Database

Create an empty MySQL database and a user with access to it, for example `expirytrack`.

Store the connection string with the .NET Secret Manager, so it is not saved in the repository:

```bash
cd backend/ExpiryTrack.Api
dotnet user-secrets set "ConnectionStrings:Default" "server=localhost;port=3306;database=expirytrack;user=YOUR_USER;password=YOUR_PASSWORD"
```

Create the tables:

```bash
dotnet ef database update
```

## 2. Backend

```bash
cd backend/ExpiryTrack.Api
dotnet run
```

The API runs at http://localhost:5226 (Swagger UI: http://localhost:5226/swagger).

On the first start, the database is filled with demo data.

## 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:5173. Requests to `/api` are forwarded to the backend automatically.

## Demo accounts

| Role | Email | Password |
|---|---|---|
| Administrator | admin@test.com | Admin123! |
| Finance Officer | finance@test.com | Finance123! |
| Department Manager | manager@test.com | Manager123! |
| Employee | employee@test.com | Employee123! |

## Tests

```bash
dotnet test
```

```bash
cd frontend
npx vitest run
```