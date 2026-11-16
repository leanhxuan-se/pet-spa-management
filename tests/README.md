# Authentication smoke tests

Run from the repository root:

```powershell
dotnet run --project tests/PetSpa.IntegrationTests/PetSpa.AuthenticationSmokeTests.csproj
```

This executable starts a local HTTP server with synthetic accounts and fake repositories. It exercises the login handlers, password rehashing, JWT bearer middleware, named and fallback policies, Customer, Pet, Booking and Category endpoints, generated OpenAPI and Operation migration metadata. It runs without a database. A failed assertion exits with an exception. On success it prints the number of checks.

The executable compiles the internal customer login types from their production source files so the fixtures can replace their dependencies.
