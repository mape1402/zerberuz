# Zerberuz Release Process

This document defines the release checklist for Zerberuz packages.

## Versioning

Zerberuz uses semantic versioning for engine packages.

- Patch versions contain compatible bug fixes.
- Minor versions contain compatible features.
- Major versions contain breaking public API or behavior changes.
- Alpha versions may change public APIs while the project is still stabilizing.

Rule payloads are versioned separately from analyzer binaries.

Production releases run from a release branch:

```text
releases/v1.0.0
```

The workflow creates or updates the matching tag after validating the branch and changelog.

## Release Checklist

Before publishing a release:

1. Create a branch named `releases/vX.Y.Z` from `main`.
2. Update `CHANGELOG.md` with a `## [vX.Y.Z]` section.
3. Run `dotnet restore`.
4. Run `dotnet build --configuration Release`.
5. Run `dotnet test --configuration Release --no-build`.
6. Run `dotnet pack . --configuration Release --output artifacts/package`.
7. Run the build and release workflow manually from the release branch.

## Package Quality

The package build should include:

- NuGet metadata.
- README and icon assets.
- XML documentation.
- SourceLink.
- Deterministic build settings.
- Portable symbols.
- Symbols package output.

## CI

Pull requests and pushes to `main` run restore, build, and test. Publishing to NuGet is only allowed after release marker validation and uses NuGet Trusted Publishing.
