---
description: Static, read-only quality & health review of this OpenTelemetry NuGet library
argument-hint: "[path or focus area, e.g. src/... or infrastructure/helm]"
---

# Repository Evaluation · OpenTelemetryExtension.Configuration

## Context

You are reviewing **this** repository: a NuGet package that wires up
OpenTelemetry (tracing, metrics, logging) for .NET apps via a single
`AddTelemetry()` call and `appsettings.json`. `AGENTS.md` is the canonical
source of truth for layout, conventions, build/test commands and the release
process — read it first and treat any deviation between it and the code as a
finding.

The shipped surface is small (the library targets `netstandard2.0`, `net8.0`,
`net10.0`); most risk is in **multi-targeting correctness, telemetry wiring,
test gating, and release/packaging hygiene** — not in third-party trust.

## Instructions

Perform a **static, read-only** review. Do not run code, install packages, or
execute scripts. Base every claim on repository contents; cite concrete files
and lines. Prefer explicit uncertainty over confident speculation, and separate
confirmed findings from speculation.

Scope: if `$ARGUMENTS` is non-empty, focus the review on that path or area;
otherwise evaluate the whole repository.

## Evaluation criteria

For each category: assign a score 1–10, justify concisely with file/line
evidence, and note uncertainty.

### 1. .NET code quality & conventions
- Adherence to `AGENTS.md` / `CLAUDE.md` conventions: file-scoped namespaces,
  `_camelCase`/`s_camelCase` fields, `is null`/`is not null`,
  `ArgumentNullException.ThrowIfNull`, expression-bodied members, no `#region`.
- `src/Directory.Build.props` invariants actually hold: nullable enabled,
  `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, implicit usings.
- Readability, correctness, internal consistency.

### 2. Multi-targeting correctness
- `netstandard2.0` does not use net5.0+ APIs without `#if NET5_0_OR_GREATER` /
  `#if !NETSTANDARD2_0` guards.
- ASP.NET Core instrumentation is referenced only where valid (not on
  `netstandard2.0`); any `!`-suppression carries the documented justification.

### 3. Telemetry wiring
- `TelemetryServiceCollectionExtensions` / `TelemetryOptions`: OTLP exporter
  configured correctly per signal (gRPC posts to base endpoint; HttpProtobuf
  appends `v1/traces|metrics|logs`), sampler, resource/service attributes,
  validation of required options.
- Config binding (`Telemetry` section) matches the documented options and
  defaults.

### 4. Tests
- Unit tests carry `[Trait("Category", "Unit")]` (untagged tests are silently
  skipped by CI); integration tests carry `[Trait("Category", "Integration")]`.
- Unit tests avoid `Thread.Sleep`/network; integration tests (`OpenObserve`,
  `SigNoz`, SQL Server) are isolated from CI and self-contained.
- Coverage of the public surface and meaningful assertions (not smoke-only).

### 5. Public API & packaging
- Public surface is minimal and intentional; XML `<summary>` docs present on
  public members, absent on internal/private.
- NuGet metadata, SemVer discipline, and that `<Version>` is managed only via
  the `prepare-release` skill (never bumped manually).

### 6. Build, CI & automation surface
- `ci.yml` builds + runs `Category=Unit`; `deploy-nuget.yml` is manual-only and
  builds only library + unit-test projects.
- Local-execution surface under `infrastructure/helm/` (install `.cmd`/`.sh`
  scripts, the `signoz.bootstrap-admin` Job) and `.agents/` (skills, commands,
  `settings.local.json`): note anything that runs implicitly or with elevated
  trust, and confirm it is documented.

## Checklist (answer each explicitly)

- Builds clean under `TreatWarningsAsErrors` (no evidence of suppressed/ignored
  warnings)
- `netstandard2.0` API guards present where required
- Every unit-test class is `Category=Unit`
- OTLP per-signal endpoint logic is correct
- Public members documented; internals not over-documented
- `<Version>` not hand-edited outside the release skill
- Scripts / Jobs that execute locally are documented and scoped
- Secrets/credentials: none committed **except** clearly local-only dev
  defaults (e.g. the SigNoz/OpenObserve dev login used by integration tests) —
  flag any real secret

Briefly explain any item that fails.

## Findings format

### A. Confirmed issues
File · line · description · suggested minimal fix.

### B. Likely issues / needs manual check
Mark each as *likely* or *unclear*.

### C. Convention drift vs AGENTS.md
List mismatches between documented rules and the code.

## Overall assessment

- **Score:** X / 10
- **Recommendation:** one of — *Healthy* · *Healthy with caveats* ·
  *Needs cleanup before release* · *Blocking issues*
- If *Blocking*, name the blocker(s).

## Suggested improvements

Specific, minimal changes that would raise quality or unblock a release
(convention fixes, missing guards, test gaps, doc/packaging clarifications).

---

FOCUS: <FOCUS>$ARGUMENTS</FOCUS>
(empty = evaluate the whole repository)
