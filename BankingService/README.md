# BankingService

ASP.NET Core Minimal API backed by SQLite and Entity Framework Core.

## Run

```powershell
dotnet run --urls http://localhost:5078
```

Swagger is available at `http://localhost:5078/swagger` in Development mode.

## Endpoints

### Create account

`POST /api/accounts`

```json
{
  "customerName": "Ada Lovelace",
  "initialDeposit": 1000.0
}
```

### Check balance

`GET /api/accounts/{id}/balance`

### Withdraw

`POST /api/accounts/{id}/withdraw`

```json
{
  "amount": 250.0
}
```

The SQLite database is created as `banking.db` on first startup. Account transactions are stored alongside the current balance.
