# Zerberuz Diagnostics

Zerberuz diagnostics use the `ZBZ` prefix.

## ID Ranges

```text
ZBZ001-ZBZ099 Naming
ZBZ100-ZBZ199 Folder and namespace structure
ZBZ200-ZBZ299 Architecture boundaries
ZBZ300-ZBZ399 Dependency rules
ZBZ400-ZBZ499 Project configuration
ZBZ900-ZBZ999 Analyzer internal/configuration diagnostics
```

## Help Pages

Each diagnostic should have:

- A short analyzer message.
- A stable help URL.
- A detailed help page.
- A bad example.
- A good example.
- A step-by-step fix.
- Suppression guidance.

Example:

```text
ZBZ001: Interface 'Repository' must start with 'I'.
Help: https://docs.zerberuz.dev/diagnostics/ZBZ001
```
