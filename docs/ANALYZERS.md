# Zerberuz Analyzers

This document tracks analyzer implementation details and diagnostic behavior.

## Status

The analyzer package shell has been defined. Implementation should start with the smallest useful vertical slice:

- Load a local rule cache.
- Apply one naming rule.
- Apply one folder structure rule.
- Emit diagnostics with help links.
- Keep analyzer execution offline and deterministic.
