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
