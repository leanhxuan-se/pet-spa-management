# Authentication smoke tests

Run from the repository root:

```powershell
dotnet run --project tests/PetSpa.IntegrationTests/PetSpa.AuthenticationSmokeTests.csproj
```

This executable starts a local HTTP server with synthetic accounts and fake repositories/services. It exercises the real login handlers, password hasher, JWT bearer middleware, role policies, customer/pet authorization, and operation model/migration metadata. It never connects to a database. A failed assertion exits with an exception; success prints the number of checks.

Internal customer login types and the resource authorization helper are compiled directly from production source files so the fixtures can replace their dependencies without changing their public visibility.
