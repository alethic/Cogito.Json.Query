# Cogito.Json.Query

[![Build](https://github.com/alethic/Cogito.Json.Query/actions/workflows/Cogito.Json.Query.yml/badge.svg)](https://github.com/alethic/Cogito.Json.Query/actions/workflows/Cogito.Json.Query.yml)

A small query language over JSON, compiled to a .NET delegate.

## Packages

**[Cogito.Json.Query](https://www.nuget.org/packages/Cogito.Json.Query)** — A small query language over JSON, compiled to a .NET delegate.

Each package carries its own README with the detail; the links above go to nuget.org.

## Building

```shell
dotnet restore Cogito.Json.Query.slnx
dotnet msbuild -p:Configuration=Release Cogito.Json.Query.dist.msbuildproj
```

Packages are staged into `dist/nuget` and test suites into `dist/tests`; run a suite with
`dotnet test -f <tfm> <path to its assembly>`.

## License

MIT — see [LICENSE](LICENSE).
