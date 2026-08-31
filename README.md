# FunnySharp

FunnySharp is a pragmatic, BCL-first functional-programming library for .NET 10 and later.
The repository is currently an intentionally API-free foundation: feature APIs are added only
when a later goal defines and verifies them.

The authoritative design and dependency boundaries are recorded in the
[product contract](docs/product-contract.md).

## Verify

```shell
dotnet restore FunnySharp.slnx
dotnet build FunnySharp.slnx --configuration Release --no-restore
dotnet test FunnySharp.slnx --configuration Release --no-build
dotnet pack FunnySharp.slnx --configuration Release --no-build --output artifacts/packages
```
