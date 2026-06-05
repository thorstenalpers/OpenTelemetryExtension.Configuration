# Contributing

Contributions are welcome — bug fixes, improvements, and ideas alike.

## Before You Start

For anything beyond a small fix, please [open an issue](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/issues/new/choose) first so we can align before you invest time in a pull request.

## Workflow

1. Fork the repository and create a branch from `develop`.
2. Make your changes.
3. Add or update tests — every public API change needs test coverage.
4. Run the build and tests locally.
5. Open a pull request against `develop`.

## Build & Test

```bash
dotnet build src/OpenTelemetryExtension.slnx -c Release
dotnet test  src/OpenTelemetryExtension.slnx -c Release --filter "Category!=Long-Running"
```

## Code Style

- File-scoped namespaces (`namespace Foo;`)
- Nullable reference types enabled — no `!` without a reason
- Private fields: `_camelCase`, static fields: `s_camelCase`
- No comments explaining *what* the code does — only *why* (hidden constraints, workarounds)
- EditorConfig is enforced at build time — run the build to catch style violations

## Versioning

This project uses [Semantic Versioning](https://semver.org). The version is maintained manually in [`OpenTelemetryExtension.Configuration.csproj`](./src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj).

If your PR changes the public API or behaviour, please:

1. Increment the version in the `<Version>` property:
   - `PATCH` (e.g. `1.0.2` → `1.0.3`) — bug fixes, internal changes
   - `MINOR` (e.g. `1.0.2` → `1.1.0`) — new options or features, backwards compatible
   - `MAJOR` (e.g. `1.0.2` → `2.0.0`) — breaking changes
2. Add a `release-notes/v{VERSION}.md` file describing what changed.

PRs without a version bump are fine for documentation or refactoring that has no user-visible impact.

## Adding a New Instrumentation Option

Follow the checklist in [CLAUDE.md](./CLAUDE.md#adding-a-new-instrumentation-option).

## Licensing

By submitting a pull request you agree that your contribution will be licensed under the [MIT License](./LICENSE).
