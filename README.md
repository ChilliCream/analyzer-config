# ChilliCream AnalyzerConfig

ChilliCream analyzers and [analyzer configurations](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers) for C# projects.

## Installation

The analyzer configuration is published to [NuGet](https://www.nuget.org/packages/ChilliCream.AnalyzerConfig.Libraries).

For libraries, install the configuration together with the Roslynator analyzers it configures:

```sh
dotnet add package ChilliCream.AnalyzerConfig.Libraries
dotnet add package Roslynator.Analyzers
dotnet add package Roslynator.CodeAnalysis.Analyzers
dotnet add package Roslynator.Formatting.Analyzers
```

The configuration package installs the ChilliCream analyzers (`ChilliCream.Analyzers`) as a dependency.

Because the package is a development dependency, NuGet automatically references it with `PrivateAssets="all"`, so it does not flow to consumers of your library.

The code-style analyzer rules only run in the IDE by default. To also enforce them during `dotnet build` and in CI, set `EnforceCodeStyleInBuild`. The rule for unnecessary using directives (IDE0005) runs during a build only when `GenerateDocumentationFile` is also set:

```xml
<PropertyGroup>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

`GenerateDocumentationFile` also reports public members that have no XML documentation comment (CS1591). To turn that off, add `<NoWarn>$(NoWarn);CS1591</NoWarn>` to the same property group.

## Analyzers

| Rule | Title |
| ---- | ----- |
| [CCA0001](https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0001.md) | Simplify name |
| [CCA0002](https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0002.md) | Remove redundant type arguments |
| [CCA0003](https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0003.md) | Access members through their declaring type |

These rules run during `dotnet build` without `EnforceCodeStyleInBuild`. The `ChilliCream.AnalyzerConfig.Libraries` package sets their severity to `warning`.

The analyzers need the .NET 10 SDK or later, and older SDKs skip them with warning CS9057.

## Versioning

`ChilliCream.Analyzers` and `ChilliCream.AnalyzerConfig.Libraries` are released together under one version number, which follows [Semantic Versioning](https://semver.org/). A new warning does not count as a breaking change:

- **Major** releases remove or rename an analyzer, a rule ID, or a package, raise the .NET SDK or compiler version the analyzers need, or change the package's MSBuild properties and targets in a way that changes compilation.
- **Minor** releases add analyzers, code fixes, or options, and change which rules the configuration enables: a rule set to `warning`, lowered, turned off, or removed from the configuration so that it falls back to its default severity. An analyzer that starts reporting in more places also ships in a minor release.
- **Patch** releases fix false positives, false negatives, code fixes, crashes, documentation, and packaging.

The configuration sets no rule above `warning`, so whether a warning fails the build is up to the project's `TreatWarningsAsErrors` setting. Any release can add warnings, so a project that builds with `TreatWarningsAsErrors` pins the version and updates it deliberately. The release notes list every rule whose severity changed, and this command applies the code fixes of the ChilliCream rules:

```sh
dotnet format analyzers --diagnostics CCA0001 CCA0002 CCA0003 --severity info
```
