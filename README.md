# AnchorBank

A banking API covering accounts, money movement and airtime. Customers register and log in, open
accounts, deposit, withdraw, transfer between accounts, and buy airtime through VTpass.

Built to learn the shape of a real API, and to be able to explain every part of it.

.NET 10, ASP.NET Core controllers, SQL Server with Dapper, JWT auth, PBKDF2 password hashing,
xUnit for the tests. No EF Core.

## Layout

Four projects. Api holds the controllers, the auth wiring and Swagger. Application has the
services, request records and interfaces. Domain has the entities and enums, and that's where the
rules live. Infrastructure has the Dapper repositories, the SQL scripts and the VTpass client.

Tests are one project per layer, plus one that boots the whole API.

Api talks to Infrastructure, Infrastructure to Application, Application to Domain. Nothing points
back the other way.

## Getting it running

SQL Server 2022 in Docker. Create the AnchorBank database, then run the scripts in
`AnchorBank.Infrastructure/Database/Scripts` in order.

The connection string and keys aren't in the repo, they go in user secrets:

```
dotnet user-secrets set "Jwt:SigningKey" "<at least 32 characters>" --project AnchorBank.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=AnchorBank;User Id=sa;Password=<password>;TrustServerCertificate=True;" --project AnchorBank.Api
dotnet user-secrets set "Vtpass:ApiKey" "<sandbox api key>" --project AnchorBank.Api
dotnet user-secrets set "Vtpass:SecretKey" "<sandbox secret key>" --project AnchorBank.Api
```

Then `dotnet run --project AnchorBank.Api` and open `https://localhost:7176/swagger`.

One gotcha worth knowing: paste only the token into Swagger's Authorize box. The document already
declares the bearer scheme, so if you type Bearer in front you send `Bearer Bearer eyJ...` and
every call comes back 401 with `invalid_token`. It looks like an auth bug in your own code and it
isn't one.

## Endpoints

Register and login are public and rate limited: 5 registrations per 10 minutes, 5 logins a minute.
Everything else needs the token.

- `POST /api/auth/register` and `POST /api/auth/login`
- `POST /api/account` creates one, the owner comes from the token and not the body
- `GET /api/account` lists mine, `GET /api/account/{id}` returns one of them
- `GET /api/account/{id}/balance`
- `GET /api/account/{id}/transactions?page=1&pageSize=20`
- `POST /api/account/{id}/deposit` and `POST /api/account/{id}/withdraw`
- `POST /api/account/{sourceId}/transfers`
- `POST /api/account/{id}/airtime`
- `GET /api/customer/{id}` returns my own record

Ownership is a WHERE clause in the queries rather than a check in a service, so asking for someone
else's account gives a 404 and not a 403.

## Moving money

Deposits, withdrawals and transfers each run in one SQL transaction: lock the account rows with
UPDLOCK and ROWLOCK, check the locked values, update, write the transaction row, commit. A
transfer locks both accounts in a single query ordered by Id. Without that ordering, two transfers
in opposite directions deadlock each other. I only believed it once the concurrency test failed.

## Airtime

The account is debited before VTpass is called, so the purchase row exists by the time the
provider answers. Debit the account and insert the purchase as Pending in one transaction, call
the provider, settle the row with the answer, and refund only when the answer is explicitly
failed. Processing and unknown stay on the row for later.

delivered maps to success, failed and reversed to failed, pending and initiated to processing,
everything else to unknown. That last bucket catches VTPass's 028 (product not whitelisted, which
comes back with no transaction object at all) and anything that isn't JSON. A connection failure
or a timeout also comes back as unknown instead of throwing, so the caller always gets a decision.

## Still to do

A purchase that comes back processing or unknown just sits there. The requery call exists on the
provider interface but nothing runs it yet, so those rows want something to reconcile them.

Transfers are internal only. The destination bank code is stored but there's no external rail
behind it. No idempotency keys on the money endpoints either, and no refresh tokens.

## Tests

```
dotnet test AnchorBank.slnx
```

Domain tests cover the entity rules, including which airtime statuses are allowed to follow which.
Application tests use small fakes written into the test files, since there's no mocking library in
the solution.
Infrastructure tests hit a real SQL Server: the fixture creates and drops AnchorBankTests from the
scripts, so a new script gets picked up by itself. The API tests boot the real pipeline with
WebApplicationFactory and mint tokens directly, which keeps the login rate limiter out of the way.

The VTpass client is tested with a fake HttpMessageHandler against JSON I captured from the
sandbox, one real success and one real failure.

Known gaps: nothing tests AirtimeService end to end, and the double refund guard is only proven by
reading it.
