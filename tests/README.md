# Authentication smoke tests

Set `PETSPA_AUTH_TEST_CONNECTION` to a fresh disposable PostgreSQL database. Run from the repository root.

```powershell
dotnet run --project tests/PetSpa.IntegrationTests/PetSpa.AuthenticationSmokeTests.csproj
```

The executable applies the legacy migrations, inserts synthetic records, and upgrades them to the current schema. It exercises the production Auth routes, profile contracts, JWT validation, account management and staff-shift lifecycle over HTTP. It also verifies generated OpenAPI and the Customer, Pet, Booking and Resource authorization checks using test repositories. Use a fresh database for each run.

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
