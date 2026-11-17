# AnchorBank

A banking API I'm building to learn ASP.NET Core properly. Customers register, open accounts,
move money, and buy airtime through VTpass. Every layer is written by hand — no scaffolding
beyond the project templates.

## Stack

- .NET 10 / ASP.NET Core, controllers
- SQL Server 2022 in Docker, reached with Dapper and Microsoft.Data.SqlClient (no EF Core)
- JWT bearer auth, PBKDF2 password hashing
- xUnit, with WebApplicationFactory for the API tests

## Projects

| Project | What's in it |
| --- | --- |
| `AnchorBank.Api` | controllers, auth pipeline, Swagger, DI |
| `AnchorBank.Application` | services, request/response records, interfaces |
| `AnchorBank.Domain` | entities and enums — the invariants live here |
| `AnchorBank.Infrastructure` | Dapper repositories, SQL scripts, VTpass client |
| `*.Tests` | one test project per layer, plus one that hosts the API |

Dependencies point inwards only: Api → Application → Domain, Infrastructure → Application → Domain.

## Running it

1. Start SQL Server:

   ```
   docker run -d --name anchorbank-sql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<strong-password>" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
   ```

2. Create the `AnchorBank` database and run the scripts in
   `AnchorBank.Infrastructure/Database/Scripts` in order, 001 to 008.

3. Put the secrets in user secrets (they are not in the repo):

   ```
   dotnet user-secrets set "Jwt:SigningKey" "<at least 32 characters>" --project AnchorBank.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=AnchorBank;User Id=sa;Password=<password>;TrustServerCertificate=True;" --project AnchorBank.Api
   dotnet user-secrets set "Vtpass:ApiKey" "<sandbox api key>" --project AnchorBank.Api
   dotnet user-secrets set "Vtpass:SecretKey" "<sandbox secret key>" --project AnchorBank.Api
   ```

4. `dotnet run --project AnchorBank.Api`, then open `https://localhost:7176/swagger`.

   In Swagger's Authorize dialog paste only the token from the login response — the document
   already declares the `bearer` scheme, so typing `Bearer` in front makes the header
   `Bearer Bearer eyJ...` and every call comes back 401 `invalid_token`.

## Endpoints

Public, rate limited per IP (5 registrations per 10 minutes, 5 logins per minute):

- `POST /api/auth/register` → 201
- `POST /api/auth/login` → 200 with `accessToken`

Everything below needs `Authorization: Bearer <token>`:

- `POST /api/account` — create an account; the owner comes from the token, never the body
- `GET /api/account` — my accounts
- `GET /api/account/{id}` — one of my accounts
- `GET /api/account/{accountId}/balance`
- `GET /api/account/{accountId}/transactions?page=1&pageSize=20`
- `POST /api/account/{accountId}/deposit`
- `POST /api/account/{accountId}/withdraw`
- `POST /api/account/{sourceAccountId}/transfers`
- `POST /api/account/{accountId}/airtime`
- `GET /api/customer/{id}` — my own record

Ownership is a `WHERE` clause on every query, not a check in a service. Asking for somebody
else's account returns 404, not 403.

## How the money moves

Deposits, withdrawals and transfers run inside one SQL transaction: `SELECT ... WITH (UPDLOCK, ROWLOCK)`
on the account rows, validate against the locked values, update, insert the transaction row, commit.
A transfer locks both accounts in ascending `Id` order in a single query, so two transfers in
opposite directions queue up instead of deadlocking.

## Airtime

The account is debited before the provider is called, and the purchase row records what happened:

1. debit the account and insert the purchase as `Pending`, in one transaction
2. call VTpass
3. settle the row with the provider's answer
4. refund, but only on an explicit `Failed` — `Processing` and `Unknown` are left alone

Provider answers are mapped like this: `delivered` → Success, `failed`/`reversed` → Failed,
`pending`/`initiated` → Processing, and anything else → Unknown. That "anything else" covers
VTpass's `028` (product not whitelisted, which carries no transaction object at all) and bodies
that aren't JSON. A connection failure or a timeout also comes back as Unknown rather than
throwing, so the caller always gets a decision.

## What isn't finished

- **Requery.** A purchase left `Processing` or `Unknown` stays that way. There is a
  `/api/requery` call on the provider interface, but nothing reconciles those rows yet.
- **Transfers** are internal only. The destination bank code is stored, but there is no external rail.
- No idempotency keys on the money endpoints, no refresh tokens, no admin surface.

## Tests

```
dotnet test AnchorBank.slnx
```

71 tests at the time of writing:

- **Domain** — entity invariants, including the airtime status transitions
- **Application** — services against hand-written fakes; there is no mocking library in the solution
- **Infrastructure** — repositories against a real SQL Server; the fixture creates and drops
  `AnchorBankTests` from the scripts, so a new script is picked up automatically
- **Api** — hosts the real pipeline with `WebApplicationFactory` and mints tokens directly so the
  login rate limiter is never in the way

The VTpass client is tested with a fake `HttpMessageHandler` and the JSON I captured from the
sandbox, including one real success and one real failure payload.

## Things worth knowing

- Configuration in `appsettings.json` is only issuer, audience, token lifetime and the VTpass base URL.
  Connection string and keys live in user secrets and are read the same way in tests and at runtime.
- SQL scripts are numbered and applied in order; there are no migrations.
- The test connection can be pointed elsewhere with `ANCHORBANK_TEST_CONNECTION`.
