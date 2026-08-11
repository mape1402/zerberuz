# Zerberuz Architecture

This document captures the intended architecture for Zerberuz.

## Goals

- Keep Roslyn analysis fast, deterministic, and offline-capable.
- Separate analyzer execution from remote rule governance.
- Use declarative rule definitions instead of executable remote code.
- Provide clear diagnostics with detailed help pages.
- Support CI adoption with pinned rule versions.

## Project Shape

- `src/Zerberuz.Analyzers` contains the Roslyn analyzer package.
- `src/Zerberuz.Analyzers.Core` contains shared analyzer execution logic.
- `src/Zerberuz.Analyzers.Rules` contains rule contracts, validation, and normalization.
- `src/Zerberuz.Analyzers.Configuration` contains local config and cache loading.
- `src/Zerberuz.Cli` contains sync, explain, doctor, and validation commands.
- `src/Zerberuz.Server` contains the remote rule and help API.
- `src/Zerberuz.Server.Contracts` contains shared payload contracts.
- `tests` contains analyzer, CLI, rule, configuration, and server tests.
- `samples` contains runnable analyzer adoption examples.
- `benchmarks` contains analyzer performance scenarios.

## Runtime Model

```text
zerberuz sync-rules
  -> downloads declarative rules
  -> validates schema and compatibility
  -> verifies hash/signature
  -> writes local cache atomically

dotnet build / IDE analysis
  -> analyzer reads local cache
  -> analyzer applies immutable compiled rule state
  -> analyzer reports diagnostics with help links
```

## Decisions

- The analyzer package does not make HTTP calls during analysis.
- The CLI owns remote synchronization.
- Rule payloads are versioned independently from analyzer binaries.
- Analyzer state is immutable or thread-safe because Roslyn runs analyzers concurrently.
- Diagnostic messages stay short; help content lives in CLI/server docs.
- Missing or invalid cache produces configuration diagnostics instead of hidden failures.

## Open Questions

- Which storage provider should the first server implementation use?
- Should private diagnostic help pages require authenticated browser sessions?
- Which rule types should receive code fixes in the first public release?
