# CLAUDE.md

[AGENTS.md](./AGENTS.md) is the single source of truth for this repository:
project layout, build & test commands, code conventions, test rules, and the
release process all live there. Read it before making changes — do not
duplicate its content here.

## Claude-specific notes

- Quick start: `dotnet build OpenTelemetryExtension.slnx -c Release`, then
  `dotnet test src/OpenTelemetryExtension.Configuration.Tests -c Release --filter "Category=Unit"`.
- GitHub Flow: branch off `main` (`feature/*` or `fix/*`) → PR → `main`; never
  commit straight to `main`. Release branches (`release/*`) are skill-managed.
- Use the `/prepare-release` skill (`.claude/skills/prepare-release/`) for the
  entire release workflow — never bump `<Version>` or publish manually.
