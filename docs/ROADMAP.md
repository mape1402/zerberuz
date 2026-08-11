# Zerberuz Roadmap

Zerberuz is the Elysium analyzer and code governance platform.

## Vision

Zerberuz should let teams define code standards once and enforce them consistently across repositories without republishing analyzer binaries for every rule change.

The platform should own:

- Roslyn analyzer execution.
- Rule schema and validation.
- Local deterministic rule cache.
- CLI synchronization and diagnostics tooling.
- Remote profile/version governance.
- Diagnostic help pages.
- CI-friendly locked rule versions.
- Performance benchmarks for analyzer overhead.

## Core Principles

- Do not call the network from analyzer hot paths.
- Do not download executable rule logic.
- Validate remote payloads before they become build inputs.
- Prefer immutable analyzer state.
- Use the narrowest Roslyn callbacks possible.
- Explain every diagnostic with examples and fixes.

## Milestones

### v0.1 - Foundation

- Create solution structure.
- Add package metadata and release workflow.
- Define rule contracts.
- Define diagnostic ID ranges.
- Add analyzer test harness.
- Add initial docs.

### v0.2 - Local Analyzer MVP

- Load local `.zerberuz/rules-cache.json`.
- Implement naming rule diagnostics.
- Implement folder structure diagnostics.
- Add generated-code exclusions.
- Add analyzer performance baseline.

### v0.3 - CLI And Cache

- Add `zerberuz init`.
- Add `zerberuz sync-rules`.
- Add `zerberuz doctor`.
- Add `zerberuz explain`.
- Add atomic cache writes.
- Add offline help cache.

### v0.4 - Server MVP

- Add rule profile endpoints.
- Add versioned rule payloads.
- Add diagnostic help endpoints.
- Add validation endpoint.
- Add hash/signature metadata.

### v0.5 - Architecture Rules

- Add namespace dependency rules.
- Add assembly dependency rules.
- Add layer maps.
- Add dependency graph tests.

### v1.0 - First Stable Release

- Stable analyzer package.
- Stable CLI commands.
- Stable rule schema.
- Server-backed profiles and help pages.
- CI examples.
- NuGet release.

## Immediate Next Steps

1. Add empty project shells and restore/build validation.
2. Define the first rule schema contracts.
3. Implement analyzer test harness.
4. Build the first naming rule vertical slice.
5. Add `zerberuz explain ZBZ001` using cached help content.
