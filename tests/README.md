# Authentication smoke tests

Run from the repository root:

```powershell
dotnet run --project tests/PetSpa.IntegrationTests/PetSpa.AuthenticationSmokeTests.csproj
```

This executable starts a local HTTP server with synthetic accounts and fake repositories. It exercises the login handlers, password rehashing, JWT bearer middleware, named and fallback policies, Customer, Pet, Booking and Category endpoints, generated OpenAPI and Operation migration metadata. It runs without a database. A failed assertion exits with an exception. On success it prints the number of checks.

The executable compiles the internal customer login types from their production source files so the fixtures can replace their dependencies.

## Booking provider acceptance tests

Set `PETSPA_BOOKING_TEST_CONNECTION` to the connection string for a dedicated PostgreSQL test database, then run from the repository root.

```powershell
dotnet run --project tests/PetSpa.BookingProviderTests/PetSpa.BookingProviderTests.csproj
```

The test applies the Customer, Resource, and Booking migrations, starts a loopback HTTP host with synthetic accounts, and writes fixtures to the test database. Mock Operation and Billing callers use their own tables and PostgreSQL transactions. The checks cover preserved prices, role and state rules, handoff, payment, rollback, row locks, and cursor pagination.
