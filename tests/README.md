# Authentication smoke tests

Set `PETSPA_AUTH_TEST_CONNECTION` to a fresh disposable PostgreSQL database. Run from the repository root.

```powershell
dotnet run --project tests/PetSpa.IntegrationTests/PetSpa.AuthenticationSmokeTests.csproj
```

The executable applies the current initial migrations and inserts synthetic records, including legacy password hashes to verify their upgrade on login. It exercises authentication, staff shifts, availability and service-session handoff, start, completion, cancellation and group tracking over HTTP. It also checks migration of an existing session and rollback across the Operation and Booking modules. Regression checks cover concurrent registration, phone collisions across account types, logout on multiple devices, and rejection of old tokens after restoring a profile or role. It also verifies generated OpenAPI and the Customer, Pet, Booking and Resource authorization checks using test repositories. Use a fresh database for each run.

## Operation smoke tests

```powershell
dotnet run --project tests/PetSpa.OperationSmokeTests/PetSpa.OperationSmokeTests.csproj
```

Exercises room and staff availability through a loopback HTTP server with synthetic accounts and repositories.

## Booking provider acceptance tests

Set `PETSPA_BOOKING_TEST_CONNECTION` to the connection string for a dedicated PostgreSQL test database, then run from the repository root.

```powershell
dotnet run --project tests/PetSpa.BookingProviderTests/PetSpa.BookingProviderTests.csproj
```

The test applies the Customer, Resource, and Booking migrations, starts a loopback HTTP host with synthetic accounts, and writes fixtures to the test database. Mock Operation and Billing callers use their own tables and PostgreSQL transactions. The checks cover preserved prices, role and state rules, handoff, payment, rollback, row locks, and cursor pagination.
