# Zerberuz

[![Build](https://github.com/mape1402/zerberuz/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/mape1402/zerberuz/actions/workflows/build-and-release.yml)
[![NuGet](https://img.shields.io/nuget/v/Zerberuz.Analyzers.svg)](https://www.nuget.org/packages/Zerberuz.Analyzers)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Zerberuz** is a configurable Roslyn analyzer platform for .NET teams. It installs as a stable analyzer engine and applies versioned rule definitions for naming conventions, folder structure, namespace layout, architecture boundaries, and dependency governance.

The analyzer is designed to be fast and deterministic: it reads local, validated rule caches during IDE/build analysis. Remote synchronization is handled by the CLI and server, not by analyzer hot paths.

## Packages

```bash
dotnet add package Zerberuz.Analyzers
```

Planned supporting packages:

```bash
dotnet tool install --global Zerberuz.Cli
dotnet add package Zerberuz.Server.Contracts
```

| Package | Purpose |
| --- | --- |
| `Zerberuz.Analyzers` | Roslyn analyzer package installed by consuming projects. |
| `Zerberuz.Cli` | Rule sync, cache validation, diagnostics explanation, and CI tooling. |
| `Zerberuz.Server.Contracts` | Shared contracts for rule payloads and diagnostic help. |
| `Zerberuz.Server` | Rule governance API and diagnostic help center. |

## Big Picture

```text
Analyzer package = execution engine
Remote rules = policy
Local cache = deterministic analysis input
CLI = sync, validation, diagnostics, CI integration
Server = governance, versioning, distribution
```

Zerberuz should download declarative rule definitions, not executable analyzer logic.

## Getting Started

Initialize a repository:

```bash
zerberuz init --team elysium --profile backend-clean-architecture
zerberuz sync-rules --source ./rules/backend-clean-architecture.json
dotnet build
```

The CLI writes rules into a shared machine/team cache, then the analyzer reads that local cache through `zerberuz.json`. No analyzer callback calls the network.

Default shared cache resolution:

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

Shared cache layout:

```text
<cacheRoot>/teams/<team>/profiles/<profile>/versions/<rulesVersion>/rules-cache.json
<cacheRoot>/teams/<team>/profiles/<profile>/versions/<rulesVersion>/help/ZBZ001.md
<cacheRoot>/teams/<team>/profiles/<profile>/latest-compatible.json
```

The analyzer reports diagnostics such as:

```text
ZBZ001: Interface 'Repository' must start with 'I'.
ZBZ100: Service class 'OrderService' must be placed under a Services folder.
```

Each diagnostic should include a help link and be explainable from the CLI:

```bash
zerberuz explain ZBZ001
zerberuz explain ZBZ001 --offline
zerberuz doctor
```

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

The demo cache is shared at `.zerberuz/cache` for local development. Teams can point `cacheRoot` or `ZERBERUZ_CACHE_ROOT` to a common machine/team location to avoid downloading the same rules per project.

## Server Persistence

`Zerberuz.Server` uses EF Core with SQLite by default. On startup, it creates the local database if needed and seeds the initial `backend` profile.

Default database path:

```text
<server-bin>/zerberuz.db
```

Override the database with the `ConnectionStrings:Zerberuz` connection string:

```bash
dotnet run --project src/Zerberuz.Server --ConnectionStrings:Zerberuz "Data Source=C:/elysium/zerberuz-data/zerberuz.db"
```

The HTTP endpoints still read through `IProfileRuleStore`, so the API surface stays stable while the persistence provider evolves.

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
        "nameMustMatch": "^I[A-Z].*"
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
