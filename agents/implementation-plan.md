# Zerberuz.Analyzers Implementation Plan

## 1. Big Picture

Zerberuz.Analyzers is a configurable Roslyn analyzer platform for enforcing code standards across .NET repositories. Its goal is to let teams install a stable analyzer package once, then evolve rules centrally through versioned remote definitions.

The core principle is:

```text
Analyzer package = execution engine
Remote rules = policy
Local cache = deterministic analysis input
CLI = sync, validation, diagnostics, CI integration
Server = governance, versioning, distribution
```

Zerberuz should not download executable code during analysis. It should download declarative rule definitions, validate them, cache them locally, and run a deterministic analyzer engine against those definitions.

## 2. Product Goals

- Enforce naming conventions.
- Enforce namespace and folder structure.
- Enforce architectural boundaries.
- Detect forbidden dependencies.
- Support project/team profiles.
- Support centralized rule management.
- Keep IDE analysis fast and non-blocking.
- Keep CI builds deterministic and reproducible.
- Work offline using a local rule cache.
- Provide clear diagnostics with actionable messages.
- Support gradual adoption through severity levels and rule scopes.

## 3. Non-Goals

- Do not execute arbitrary remote code.
- Do not block normal IDE typing on network calls.
- Do not require the remote service for every build.
- Do not replace existing Roslyn style analyzers such as StyleCop or .NET analyzers.
- Do not couple the analyzer engine to a specific SaaS provider.
- Do not store secrets in source control.

## 4. Proposed Repository Structure

```text
zerberuz/
  src/
    Zerberuz.Analyzers/
    Zerberuz.Analyzers.Core/
    Zerberuz.Analyzers.Rules/
    Zerberuz.Analyzers.Configuration/
    Zerberuz.Cli/
    Zerberuz.Server/
    Zerberuz.Server.Contracts/
  tests/
    Zerberuz.Analyzers.Tests/
    Zerberuz.Analyzers.Core.Tests/
    Zerberuz.Analyzers.Rules.Tests/
    Zerberuz.Cli.Tests/
    Zerberuz.Server.Tests/
  samples/
    BasicNaming/
    CleanArchitecture/
    FolderStructure/
  docs/
    rules-schema.md
    diagnostics.md
    configuration.md
    performance.md
  agents/
    implementation-plan.md
```

## 5. Main Components

### Zerberuz.Analyzers

Roslyn analyzer package installed by consuming projects.

Responsibilities:

- Register analyzer actions.
- Read local configuration from `.editorconfig`, `zerberuz.json`, MSBuild properties, or AdditionalFiles.
- Load validated rules from the local cache.
- Apply diagnostics efficiently.
- Avoid network access during normal analysis.

### Zerberuz.Analyzers.Core

Shared analyzer execution engine.

Responsibilities:

- Rule matching.
- Symbol classification.
- Path and namespace normalization.
- Diagnostic construction.
- Rule severity resolution.
- Performance-safe caches.

### Zerberuz.Analyzers.Rules

Rule model and validators.

Responsibilities:

- JSON schema model.
- Rule parsing.
- Version compatibility checks.
- Validation errors.
- Rule normalization.

### Zerberuz.Analyzers.Configuration

Local configuration loader.

Responsibilities:

- Resolve rule profile.
- Resolve cache path.
- Resolve pinned rule version.
- Resolve offline/strict mode.
- Merge `.editorconfig`, `zerberuz.json`, and MSBuild options.

### Zerberuz.Cli

Developer and CI command-line tool.

Responsibilities:

- `zerberuz init`
- `zerberuz sync-rules`
- `zerberuz validate-rules`
- `zerberuz explain ZBZ001`
- `zerberuz cache status`
- `zerberuz cache clear`
- `zerberuz rules diff`
- `zerberuz doctor`

The CLI is responsible for controlled network synchronization. CI should use the CLI before `dotnet build`.

### Zerberuz.Server

Remote service for rule governance.

Responsibilities:

- Store rule profiles.
- Version rule sets.
- Serve signed/hashed rule definitions.
- Support organization/team/project scopes.
- Provide audit history.
- Provide schema compatibility metadata.

## 6. Runtime Flow

### Local Developer Flow

```text
Developer installs NuGet analyzer package
Developer runs zerberuz init
zerberuz.json is created
Developer runs zerberuz sync-rules
Rules are downloaded and cached
IDE/build analyzers read cache only
Diagnostics appear in IDE and build output
```

### CI Flow

```text
Checkout repository
Install .NET SDK
Run zerberuz sync-rules --locked
Run zerberuz validate-cache
Run dotnet build -warnaserror
Run dotnet test
```

### Offline Flow

```text
Analyzer starts
Analyzer checks local config
Analyzer loads local cache
Analyzer applies latest compatible cached rule set
If cache is missing, analyzer emits one configuration diagnostic
```

## 7. Configuration Design

### zerberuz.json

Example:

```json
{
  "profile": "backend-clean-architecture",
  "rulesVersion": "2026.08.11",
  "mode": "locked",
  "rulesEndpoint": "https://rules.zerberuz.dev",
  "cachePath": ".zerberuz/rules-cache.json",
  "diagnostics": {
    "defaultSeverity": "warning"
  }
}
```

Recommended modes:

- `locked`: use exactly the pinned rules version.
- `latest-compatible`: use the latest compatible cached version.
- `offline`: never attempt remote sync from CLI unless explicitly requested.
- `permissive`: emit configuration warnings instead of failing.

### .editorconfig Integration

`.editorconfig` should override diagnostic severity because it is already native to Roslyn and IDE tooling.

Example:

```ini
dotnet_diagnostic.ZBZ001.severity = warning
dotnet_diagnostic.ZBZ002.severity = error
dotnet_diagnostic.ZBZ003.severity = suggestion
```

## 8. Rule Definition Model

Rules should be declarative, versioned, and schema-validated.

Example:

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
    },
    {
      "id": "ZBZ002",
      "type": "folder-structure",
      "title": "Services must live in a Services folder",
      "severity": "warning",
      "target": {
        "symbolKind": "class",
        "nameMustMatch": ".*Service$"
      },
      "condition": {
        "pathMustMatch": "src/**/Services/**"
      },
      "message": "Service class '{symbolName}' must be placed under a Services folder."
    }
  ]
}
```

## 9. Diagnostic ID Strategy

Use stable diagnostic IDs. Never reuse an ID for a different meaning.

Suggested groups:

```text
ZBZ001-ZBZ099 Naming
ZBZ100-ZBZ199 Folder and namespace structure
ZBZ200-ZBZ299 Architecture boundaries
ZBZ300-ZBZ399 Dependency rules
ZBZ400-ZBZ499 Project configuration
ZBZ900-ZBZ999 Analyzer internal/configuration diagnostics
```

Each diagnostic ID should have a matching help article. The analyzer diagnostic should include a stable help link when possible:

```text
https://docs.zerberuz.dev/diagnostics/ZBZ001
```

For private/team-specific rules, the link can point to the configured Zerberuz Server:

```text
https://rules.zerberuz.dev/help/diagnostics/ZBZ001?profile=backend-clean-architecture&version=2026.08.11
```

The diagnostic message should stay short. The help article should carry the detailed explanation, examples, rationale, and solution.

## 10. Analyzer Design Best Practices

### Registration

Use the narrowest Roslyn callbacks possible.

Prefer:

- `RegisterSymbolAction` for naming and symbol-level rules.
- `RegisterSyntaxNodeAction` only when syntax shape matters.
- `RegisterCompilationStartAction` for per-compilation setup and caches.
- `RegisterCompilationAction` for final cross-symbol checks only when necessary.

Avoid:

- Scanning the full compilation repeatedly.
- Reading files repeatedly from every callback.
- Blocking on network calls.
- Creating regular expressions repeatedly.

### Initialization

Analyzer initialization should:

- Enable concurrent execution.
- Configure generated code analysis deliberately.
- Load local rules once per compilation.
- Precompile regex patterns once.
- Build lookup structures once.

Conceptual shape:

```csharp
public override void Initialize(AnalysisContext context)
{
    context.EnableConcurrentExecution();
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

    context.RegisterCompilationStartAction(startContext =>
    {
        var analyzerState = AnalyzerState.Create(startContext.Options);

        startContext.RegisterSymbolAction(
            analyzerState.AnalyzeNamedType,
            SymbolKind.NamedType);
    });
}
```

## 11. Performance Requirements

Analyzer performance is a product feature. Zerberuz should feel invisible until it finds a real issue.

### Hard Rules

- No remote HTTP calls from analyzer callbacks.
- No synchronous blocking on long I/O in analysis hot paths.
- No repeated JSON parsing per symbol.
- No repeated regex compilation per symbol.
- No broad semantic model requests unless required.
- No full solution scanning from individual project analyzers.
- No expensive allocations in tight loops.

### Caching Strategy

Use layered caches:

```text
Process-level immutable cache
  keyed by cache file path + last write time + length + schema version

Compilation-level state
  normalized rules, regex instances, diagnostic descriptors

Symbol-level local calculations
  cheap and short-lived
```

Important: cached state must be immutable or thread-safe because Roslyn analyzers run concurrently.

### Path Normalization

Normalize paths once:

- Convert backslashes to forward slashes.
- Normalize drive/case behavior on Windows.
- Use project-relative paths when available.
- Avoid allocating normalized strings repeatedly.

### Regex Strategy

- Validate regex at sync time.
- Compile regex at analyzer startup only if necessary.
- Prefer simple prefix/suffix/contains operations where possible.
- Treat regex rules as more expensive than literal rules.

### Generated Code

Default behavior should ignore generated code:

- `*.g.cs`
- `*.Designer.cs`
- `*.generated.cs`
- files with `<auto-generated />`

Allow opt-in analysis through config.

## 12. Remote Rules and Determinism

The analyzer package should not decide when to fetch rules. The CLI should.

Recommended design:

```text
zerberuz sync-rules
  -> downloads remote rules
  -> validates schema
  -> verifies hash/signature
  -> writes local cache atomically

dotnet build
  -> analyzer reads local cache
  -> analyzer reports diagnostics deterministically
```

### Cache File

Recommended path:

```text
.zerberuz/rules-cache.json
```

Recommended cache metadata:

```json
{
  "cacheFormatVersion": "1.0",
  "rulesVersion": "2026.08.11",
  "downloadedAt": "2026-08-11T00:00:00Z",
  "source": "https://rules.zerberuz.dev",
  "sha256": "...",
  "signature": "...",
  "rules": []
}
```

Write cache atomically:

```text
write temp file
flush
validate read-back
replace existing cache file
```

## 13. Security Model

### Allowed

- Download JSON rule definitions.
- Verify server TLS.
- Verify rule schema.
- Verify content hash.
- Verify optional signature.
- Use environment variables or local credential stores for tokens.

### Not Allowed

- Download and execute analyzer DLLs dynamically.
- Execute scripts from remote config.
- Deserialize polymorphic arbitrary .NET types.
- Store access tokens in `zerberuz.json`.
- Let remote config specify local filesystem paths outside the repository.

### Authentication

Use this priority:

1. CI environment variable.
2. Local user secret store.
3. OS credential manager.
4. Explicit CLI login flow.

Never require the analyzer itself to know long-lived secrets.

## 14. Server API Proposal

### Endpoints

```text
GET /api/v1/profiles/{profile}/versions
GET /api/v1/profiles/{profile}/versions/{version}
GET /api/v1/profiles/{profile}/latest-compatible?engineVersion=1.0
GET /api/v1/diagnostics/{diagnosticId}
GET /api/v1/profiles/{profile}/versions/{version}/diagnostics/{diagnosticId}/help
POST /api/v1/rules/validate
```

### Rule Response

```json
{
  "profile": "backend-clean-architecture",
  "rulesVersion": "2026.08.11",
  "minimumEngineVersion": "1.0.0",
  "schemaVersion": "1.0",
  "sha256": "...",
  "signature": "...",
  "rules": []
}
```

## 15. Diagnostic Help Center

Every Zerberuz diagnostic should have a detailed help section served by Zerberuz Server and optionally cached locally for offline use.

The goal is:

```text
Compiler/IDE diagnostic = short actionable signal
Help article = full explanation and repair guide
CLI explain command = terminal-friendly version of the help article
```

### Help Content Model

Each diagnostic help article should include:

- Diagnostic ID.
- Title.
- Severity.
- Category.
- Short summary.
- Why the rule exists.
- What triggered the diagnostic.
- Bad example.
- Good example.
- Step-by-step fix.
- Auto-fix availability.
- Configuration options.
- Suppression guidance.
- Related rules.
- Rule version and last updated date.

Example:

```json
{
  "diagnosticId": "ZBZ001",
  "title": "Interfaces must start with I",
  "category": "Naming",
  "defaultSeverity": "warning",
  "summary": "Interface names must use the configured prefix.",
  "why": "Consistent interface naming improves scanning and makes abstractions easier to identify.",
  "trigger": "An interface symbol was found whose name does not match the configured naming pattern.",
  "badExample": "public interface Repository { }",
  "goodExample": "public interface IRepository { }",
  "fix": [
    "Rename the interface so it starts with I.",
    "Update all references.",
    "Run the test suite after the rename."
  ],
  "configuration": {
    "rule": "naming.interface.mustStartWith",
    "defaultValue": "I"
  },
  "suppression": "Suppress only when interoperating with generated code or external naming contracts.",
  "related": ["ZBZ002"],
  "lastUpdated": "2026-08-11"
}
```

### Server Rendering

The server should support both machine-readable and human-readable help:

```text
GET /api/v1/diagnostics/ZBZ001
GET /api/v1/profiles/{profile}/versions/{version}/diagnostics/ZBZ001/help
GET /help/diagnostics/ZBZ001
```

Recommended output formats:

- JSON for CLI and tooling.
- HTML for browser help pages.
- Markdown for docs export.

### Analyzer Integration

Diagnostic descriptors should include `helpLinkUri`.

Rules loaded from cache may provide a profile-specific help URL. If no remote help URL exists, use the public docs URL.

Example behavior:

```text
ZBZ001: Interface 'Repository' must start with 'I'.
Help: https://rules.zerberuz.dev/help/diagnostics/ZBZ001
```

The analyzer should not fetch help content. It should only attach the link.

### CLI Integration

`zerberuz explain` should show the same help content in terminal form.

```text
zerberuz explain ZBZ001
zerberuz explain ZBZ001 --profile backend-clean-architecture --version 2026.08.11
zerberuz explain ZBZ001 --offline
```

The CLI may:

- Read help from local cache.
- Fetch help from the server.
- Render Markdown in the terminal.
- Open the browser page with `--open`.

### Offline Help Cache

`sync-rules` should optionally cache diagnostic help alongside rule definitions.

Recommended cache path:

```text
.zerberuz/help/ZBZ001.md
.zerberuz/help/ZBZ002.md
```

This gives developers useful explanations even without network access.

### Authoring Rules

Rule authors should not be able to publish a new diagnostic without help content unless the rule is marked experimental.

Minimum publish requirements:

- Stable diagnostic ID.
- Short diagnostic message.
- Help title.
- Explanation.
- Bad example.
- Good example.
- Fix guidance.

This keeps Zerberuz friendly at scale.

## 16. CLI Commands

### init

Creates starter config.

```text
zerberuz init --profile backend-clean-architecture
```

### sync-rules

Downloads rules into local cache.

```text
zerberuz sync-rules
zerberuz sync-rules --version 2026.08.11
zerberuz sync-rules --locked
```

### validate-rules

Validates a local rule file.

```text
zerberuz validate-rules .zerberuz/rules-cache.json
```

### doctor

Checks configuration and cache health.

```text
zerberuz doctor
```

### explain

Explains one diagnostic.

```text
zerberuz explain ZBZ002
```

## 17. Initial Rule Types

### Naming Rules

Targets:

- class
- interface
- enum
- method
- property
- field
- parameter
- namespace

Conditions:

- `mustStartWith`
- `mustEndWith`
- `mustContain`
- `mustMatch`
- `mustNotMatch`
- `allowedSuffixes`
- `forbiddenSuffixes`

### Folder Structure Rules

Inputs:

- file path
- project path
- namespace
- symbol kind
- symbol name

Conditions:

- `pathMustMatch`
- `pathMustNotMatch`
- `namespaceMustContain`
- `namespaceMustMatchPath`

### Architecture Rules

Inputs:

- containing namespace
- referenced namespace
- referenced assembly
- project name

Conditions:

- `layerMayReference`
- `layerMustNotReference`
- `namespaceMustNotReference`
- `assemblyMustNotReference`

### Dependency Rules

Inputs:

- package references
- assembly references
- namespace usage

Conditions:

- forbidden package
- forbidden assembly
- forbidden namespace
- allowed dependency map

## 18. Testing Strategy

### Analyzer Tests

Use `Microsoft.CodeAnalysis.CSharp.Testing`.

Test:

- Diagnostic appears at correct location.
- Diagnostic message is correct.
- Severity is correct.
- `.editorconfig` severity override works.
- Generated code is ignored.
- Missing cache produces a clear configuration diagnostic.
- Invalid cache produces a clear configuration diagnostic.

### Rule Tests

Test:

- Valid rules parse.
- Invalid regex is rejected.
- Unknown rule type is rejected.
- Unsupported schema version is rejected.
- Severity normalization works.

### CLI Tests

Test:

- Sync writes cache atomically.
- Locked mode refuses unexpected versions.
- Invalid remote payload does not overwrite cache.
- Doctor reports useful failures.
- Explain renders diagnostic help from remote and local cache.
- Offline explain works when cached help exists.

### Performance Tests

Add benchmark projects with:

- 100 files
- 1,000 files
- 10,000 symbols
- many naming rules
- many folder rules

Measure:

- analyzer initialization time
- per-symbol analysis cost
- memory allocations
- build time delta

## 19. Observability

Analyzer diagnostics should be enough for normal use. Avoid noisy logging from analyzers.

CLI can provide richer output:

- rules version
- cache path
- profile
- endpoint
- last sync time
- hash/signature status
- compatible analyzer engine version

Server should log:

- rule publish events
- sync requests
- diagnostic help requests
- validation failures
- auth failures
- deprecated rule usage

## 20. Packaging

### NuGet Packages

```text
Zerberuz.Analyzers
Zerberuz.Cli
Zerberuz.Server.Contracts
```

`Zerberuz.Analyzers` should include:

- analyzer DLL
- default diagnostic descriptors
- README
- package icon
- release notes

### Versioning

Use semantic versioning for engine packages:

```text
1.0.0
1.1.0
2.0.0
```

Use date or semantic versions for rule sets:

```text
2026.08.11
2026.08.11.1
1.4.0
```

Rule payload should declare:

```text
minimumEngineVersion
maximumEngineVersion, optional
schemaVersion
rulesVersion
```

## 21. Roadmap

### Phase 1: Local MVP

- Create solution structure.
- Build analyzer package.
- Load local `zerberuz.json`.
- Load local rule file.
- Implement naming rules.
- Implement folder rules.
- Add analyzer tests.
- Package as local NuGet.

### Phase 2: CLI and Cache

- Build `Zerberuz.Cli`.
- Implement `init`.
- Implement `sync-rules` from local/file endpoint.
- Implement cache metadata.
- Implement `doctor`.
- Add CI examples.

### Phase 3: Remote Server

- Build minimal ASP.NET Core API.
- Store profiles and versions.
- Serve rule payloads.
- Serve diagnostic help pages.
- Add auth.
- Add hash/signature support.
- Add rule validation endpoint.

### Phase 4: Architecture Rules

- Implement namespace dependency rules.
- Implement assembly reference rules.
- Implement layer maps.
- Add dependency graph tests.

### Phase 5: Developer Experience

- Add `zerberuz explain`.
- Add browser-rendered diagnostic help pages.
- Add offline diagnostic help cache.
- Improve diagnostic documentation.
- Add sample repositories.
- Add GitHub Actions examples.
- Add rule migration tooling.

### Phase 6: Governance

- Add audit history.
- Add approval workflow.
- Add organization/team scopes.
- Add deprecation warnings.
- Add compatibility dashboards.

## 22. Recommended First Implementation Slice

Build the smallest useful vertical slice:

```text
Zerberuz.Analyzers
  reads .zerberuz/rules-cache.json
  applies ZBZ001 naming rule
  applies ZBZ100 folder rule

Zerberuz.Cli
  init
  sync-rules from a local JSON URL/file
  doctor
  explain ZBZ001 from local help cache

tests
  analyzer diagnostics
  invalid cache
  missing cache
  diagnostic help rendering
```

This proves the entire architecture without overbuilding the server too early.

## 23. Key Engineering Decisions

- Analyzer engine must be deterministic.
- CLI owns remote sync.
- Rule config is data, not executable code.
- Cache files are immutable inputs during analysis.
- `.editorconfig` owns severity overrides.
- Rules are versioned independently from analyzer binaries.
- Performance budgets are part of acceptance criteria.
- Security checks happen before cache replacement.
- Every diagnostic should have useful human-facing help.

## 24. Definition of Done for MVP

- A sample project can install `Zerberuz.Analyzers`.
- `zerberuz init` creates config.
- `zerberuz sync-rules` creates `.zerberuz/rules-cache.json`.
- `dotnet build` emits `ZBZ001` for a naming violation.
- `dotnet build` emits `ZBZ100` for a folder violation.
- Diagnostics include help links.
- `zerberuz explain ZBZ001` shows detailed guidance.
- Analyzer does not call the network.
- Analyzer works offline.
- Analyzer tests pass.
- CLI tests pass.
- README explains setup in under five minutes.
