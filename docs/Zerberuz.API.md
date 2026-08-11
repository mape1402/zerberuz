# Zerberuz API Reference

This document should describe the public Zerberuz API as it is implemented.

## Packages

- `Zerberuz.Analyzers`
- `Zerberuz.Cli`
- `Zerberuz.Server.Contracts`
- `Zerberuz.Server`

## Planned Public Surface

- Rule payload contracts.
- Rule validation result contracts.
- Diagnostic help contracts.
- Analyzer configuration contracts.
- Cache metadata contracts.
- CLI command contracts.
- Server profile/version contracts.

## Diagnostic Help

Every diagnostic should be explainable by ID:

```bash
zerberuz explain ZBZ001
```

The server should expose equivalent machine-readable and browser-readable help.
