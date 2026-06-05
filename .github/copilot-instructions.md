# GitHub Copilot Instructions

This repo is a .NET NuGet package. Apply these rules in every suggestion.

## Language & framework
- C# with nullable reference types enabled — never use `!` to suppress nullability without a comment explaining why
- Target frameworks: `netstandard2.0` and `net10.0` — guard net5.0+ APIs with `#if NET5_0_OR_GREATER`
- xUnit for tests, Moq for mocking
- No third-party packages in the main library beyond OpenTelemetry SDK packages already referenced

## Code style
- File-scoped namespaces: `namespace OpenTelemetryExtension.Configuration;`
- No block-scoped `namespace Foo { }`
- `var` for local variables when the type is obvious from the right-hand side
- Expression-bodied members for single-line methods/properties
- Primary constructors where applicable
- Pattern matching over `is`/`as` + cast

## Tests
- xUnit `[Fact]` for single cases, `[Theory]` + `[InlineData]` for parameterised cases
- Method name pattern: `MethodOrProperty_Condition_ExpectedResult`
- Arrange / Act / Assert with a blank line between each section
- Use `ServiceCollection` + `BuildServiceProvider()` to verify DI registrations — no reflection hacks
- Use `Record.Exception` (not `Assert.Throws<T>`) when asserting that no exception is thrown

## What NOT to do
- Do not add `using` directives that are already covered by global/implicit usings
- Do not add `// TODO` comments — raise an issue instead
- Do not modify `*.Sample` project for library behaviour changes
- Do not change `<Version>` in the csproj without also creating `release-notes/v{VERSION}.md`
- Do not use `Thread.Sleep` or `Task.Delay` in tests
