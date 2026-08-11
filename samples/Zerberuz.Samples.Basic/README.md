# Zerberuz Basic Sample

This sample demonstrates the first analyzer adoption scenario.

It includes:

- `zerberuz.json` configured for team `elysium`, profile `backend`, and `latest-compatible` rules.
- `Repository`, which intentionally violates `ZBZ001`.
- `OrderService`, which intentionally violates `ZBZ100` until it lives under a `Services` folder.

From the repository root:

```bash
dotnet run --project src/Zerberuz.Server -- --urls http://localhost:5000
```

Then sync rules in another terminal:

```bash
dotnet run --project src/Zerberuz.Cli -- sync-rules --server http://localhost:5000 --profile backend --config-path samples/Zerberuz.Samples.Basic/zerberuz.json --cache-root .zerberuz/cache
```

Build the sample:

```bash
dotnet build samples/Zerberuz.Samples.Basic/Zerberuz.Samples.Basic.csproj
```

The analyzer reads the shared cache and reports the configured diagnostics. If the cache has not been synchronized yet, the sample still builds without analyzer warnings.
