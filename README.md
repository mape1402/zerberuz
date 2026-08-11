# Zerberuz

[![Build](https://github.com/mape1402/zerberuz/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/mape1402/zerberuz/actions/workflows/build-and-release.yml)
[![NuGet](https://img.shields.io/nuget/v/Zerberuz.Analyzers.svg)](https://www.nuget.org/packages/Zerberuz.Analyzers)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Zerberuz** is a configurable Roslyn analyzer platform for .NET teams. It installs as a stable analyzer engine and applies versioned rule definitions for naming conventions, folder structure, namespace layout, architecture boundaries, and dependency governance.

The analyzer is designed to be fast and deterministic: it reads local, validated rule caches during IDE/build analysis. Remote synchronization is handled by the CLI and server, not by analyzer hot paths.

## Packages

| Package | Purpose |
| --- | --- |
| `Zerberuz.Analyzers` | Roslyn analyzer package installed by consuming projects. |
| `Zerberuz.Cli` | Rule sync, cache validation, diagnostics explanation, and CI tooling. |
| `Zerberuz.Server.Contracts` | Shared contracts for rule payloads and diagnostic help. |
| `Zerberuz.Server` | Hostable ASP.NET Core package for rule governance API and diagnostic help center. |

## Big Picture

```text
Analyzer package = execution engine
Remote rules = policy
Local cache = deterministic analysis input
CLI = sync, validation, diagnostics, CI integration
Server = governance, versioning, distribution
```

Zerberuz should download declarative rule definitions, not executable analyzer logic.

## Quick Start

Zerberuz has two integration points:

1. Host `Zerberuz.Server` in an ASP.NET Core app owned by your team.
2. Install `Zerberuz.Analyzers` in each .NET project and point it at a shared rule cache with `zerberuz.json`.

The analyzer never calls the server during Roslyn analysis. The CLI syncs rules from the server into a shared cache, and the analyzer reads that local cache during IDE/build work.

## Host Zerberuz.Server

`Zerberuz.Server` is a hostable NuGet package. Your application owns hosting, authentication, authorization, observability, deployment, connection strings, and infrastructure.

Create or use an ASP.NET Core host:

```bash
dotnet new web -n Company.AnalyzerRules.Host
cd Company.AnalyzerRules.Host
dotnet add package Zerberuz.Server
```

Configure the database connection in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Zerberuz": "Data Source=zerberuz.db"
  }
}
```

Wire Zerberuz into `Program.cs`:

```csharp
using Zerberuz.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddZerberuzServer(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("Zerberuz")!);
});

var app = builder.Build();

await app.Services.InitializeZerberuzServerAsync();

app.MapZerberuzServer();
app.Run();
```

Run the host:

```bash
dotnet run --urls http://localhost:5000
```

The package maps these endpoints:

```text
GET  /api/v1/profiles/{profile}/versions
GET  /api/v1/profiles/{profile}/versions/{version}
GET  /api/v1/profiles/{profile}/latest-compatible
GET  /api/v1/diagnostics/{diagnosticId}
GET  /api/v1/profiles/{profile}/versions/{version}/diagnostics/{diagnosticId}/help
POST /api/v1/rules/validate
```

Disable the demo seed data for production hosts:

```csharp
builder.Services.AddZerberuzServer(options =>
{
    options.SeedDefaultProfiles = false;
    options.UseSqlite(builder.Configuration.GetConnectionString("Zerberuz")!);
});
```

Use a custom EF provider through the host:

```csharp
using Microsoft.EntityFrameworkCore;
using Zerberuz.Server;

builder.Services.AddZerberuzServer(options =>
{
    options.UseDbContext(db =>
    {
        db.UseSqlite(builder.Configuration.GetConnectionString("Zerberuz")!);
    });
});
```

## Configure A Project

Install the analyzer in the project that should receive diagnostics:

```bash
dotnet add package Zerberuz.Analyzers
```

Add `zerberuz.json` at the project root:

```json
{
  "team": "elysium",
  "profile": "backend",
  "rulesVersion": "latest-compatible",
  "mode": "latest-compatible",
  "rulesEndpoint": "http://localhost:5000",
  "cacheRoot": "C:/elysium/zerberuz-cache"
}
```

Include `zerberuz.json` as an analyzer additional file:

```xml
<ItemGroup>
  <AdditionalFiles Include="zerberuz.json" />
</ItemGroup>
```

The important fields are:

| Field | Purpose |
| --- | --- |
| `team` | Cache namespace for a team or organization. |
| `profile` | Rule profile served by Zerberuz Server, such as `backend`. |
| `rulesVersion` | Exact version like `2026.08.11`, or `latest-compatible`. |
| `rulesEndpoint` | Zerberuz Server base URL. |
| `cacheRoot` | Shared cache root used by all projects on the machine/team image. |

## Sync Rules

Install the CLI when published as a tool:

```bash
dotnet tool install --global Zerberuz.Cli
```

Sync rules from your hosted server:

```bash
zerberuz sync-rules --server http://localhost:5000 --profile backend --config-path zerberuz.json --cache-root C:/elysium/zerberuz-cache
```

Validate the local/shared cache:

```bash
zerberuz doctor --config-path zerberuz.json --cache-root C:/elysium/zerberuz-cache
```

Build the project:

```bash
dotnet build
```

Explain a diagnostic from the offline help synced into the cache:

```bash
zerberuz explain ZBZ001 --offline --config-path zerberuz.json --cache-root C:/elysium/zerberuz-cache
```

## Shared Cache

Use one cache root per team/machine image so every project reuses the same downloaded rules.

Resolution order:

```text
--cache-root
zerberuz.json: cacheRoot
ZERBERUZ_CACHE_ROOT
OS default
```

OS defaults:

```text
Windows: %LOCALAPPDATA%/Zerberuz/cache
Linux/macOS: ~/.zerberuz/cache
```

Layout:

```text
<cacheRoot>/teams/<team>/profiles/<profile>/versions/<rulesVersion>/rules-cache.json
<cacheRoot>/teams/<team>/profiles/<profile>/versions/<rulesVersion>/help/ZBZ001.md
<cacheRoot>/teams/<team>/profiles/<profile>/latest-compatible.json
```

## Diagnostics

The analyzer reports diagnostics from the synced rules:

```text
ZBZ001: Interface 'Repository' must start with 'I'.
ZBZ100: Service class 'OrderService' must be placed under a Services folder.
```

Diagnostic help can be served online by `Zerberuz.Server` or read offline from the synced cache.

## Local End-to-End Demo

Run these commands from the repository root to exercise the full local loop: server, CLI sync, shared cache, analyzer diagnostics, and offline help.

Start the rule server:

```bash
dotnet run --project src/Zerberuz.Server -- --urls http://localhost:5000
```

In another terminal, sync the seeded `backend` profile into the shared demo cache:

```bash
dotnet run --project src/Zerberuz.Cli -- sync-rules --server http://localhost:5000 --profile backend --config-path samples/Zerberuz.Samples.Basic/zerberuz.json --cache-root .zerberuz/cache
```

Validate the shared cache:

```bash
dotnet run --project src/Zerberuz.Cli -- doctor --config-path samples/Zerberuz.Samples.Basic/zerberuz.json --cache-root .zerberuz/cache
```

Build the sample and observe configured diagnostics:

```bash
dotnet build samples/Zerberuz.Samples.Basic/Zerberuz.Samples.Basic.csproj
```

Explain a diagnostic from the offline help cached by `sync-rules`:

```bash
dotnet run --project src/Zerberuz.Cli -- explain ZBZ001 --offline --config-path samples/Zerberuz.Samples.Basic/zerberuz.json --cache-root .zerberuz/cache
```

## Server Persistence

`Zerberuz.Server` uses EF Core. SQLite is the default provider for the current package and local demo. On startup, `InitializeZerberuzServerAsync()` creates the database if needed and optionally seeds the initial `backend` profile.

Default local database path:

```text
<server-bin>/zerberuz.db
```

## Rule Configuration

Example rule cache payload:

```json
{
  "schemaVersion": "1.0",
  "rulesVersion": "2026.08.11",
  "profile": "backend-clean-architecture",
  "rules": [
    {
      "id": "ZBZ001",
      "type": "naming",
      "title": "Interfaces must start with I",
      "severity": "warning",
      "target": {
        "symbolKind": "interface"
      },
      "condition": {
        "mustStartWith": "I",
        "mustMatch": "^I[A-Z].*"
      },
      "message": "Interface '{symbolName}' must start with 'I'."
    }
  ]
}
```

## Design Principles

- Analyzer execution must be deterministic.
- The analyzer must not call remote services from Roslyn callbacks.
- Rule definitions are data, not executable code.
- Shared local cache files are immutable inputs during analysis.
- `.editorconfig` owns severity overrides.
- Every diagnostic should have useful human-facing help.
- Performance budgets are part of acceptance criteria.

## Project Shape

```text
src/
  Zerberuz.Analyzers/
  Zerberuz.Analyzers.Core/
  Zerberuz.Analyzers.Rules/
  Zerberuz.Analyzers.Configuration/
  Zerberuz.Cli/
  Zerberuz.Server/
  Zerberuz.Server.Contracts/
tests/
samples/
benchmarks/
docs/
agents/
```

See [agents/implementation-plan.md](agents/implementation-plan.md) for the full implementation plan.
