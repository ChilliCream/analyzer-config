# ChilliCream AnalyzerConfig

ChilliCream [analyzer configurations](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers) for C# projects.

## Installation

The analyzer configuration is published to [NuGet](https://www.nuget.org/packages/ChilliCream.AnalyzerConfig.Libraries).

For libraries, install the configuration together with the Roslynator analyzers it configures:

```sh
dotnet add package ChilliCream.AnalyzerConfig.Libraries
dotnet add package Roslynator.Analyzers
dotnet add package Roslynator.CodeAnalysis.Analyzers
dotnet add package Roslynator.Formatting.Analyzers
```

Because the package is a development dependency, NuGet automatically references it with `PrivateAssets="all"`, so it does not flow to consumers of your library.

The code-style analyzer rules only run in the IDE by default. To also enforce them during `dotnet build` and in CI, set:

```xml
<PropertyGroup>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```
