using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportNothing_When_SimplifyNameRemovesCrefParameterQualifier()
    {
        // arrange
        var source = WithLib(
            """
            using Lib;

            class Sub : Base
            {
                /// <see cref="Take(Derived.Nested)"/>
                public static void Take(Base.Nested n)
                {
                }
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_SimplifyNameRemovesQualifierAndMore()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public class Nested
                {
                    public class Inner;

                    Derived.Nested.Inner field = null!;
                }
            }

            class Derived : Base;
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_SimplifyNameRemovesQualifierInDerived()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public class Nested
                {
                    public static void Make()
                    {
                    }
                }
            }

            class Derived : Base
            {
                void M()
                {
                    Derived.Nested.Make();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_SimplifyNameRemovesQualifierInType()
    {
        // arrange
        var source = WithLib(
            """
            using Lib;

            class Sub : Base
            {
                Derived.Nested item = null!;
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_SimplifyNameSetToWarning()
    {
        // arrange
        var source = WithLib(
            """
            using Lib;

            class Sub : Base
            {
                Derived.Nested item = null!;
            }
            """);

        var test = CreateTest(source);
        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            """
            [*.cs]
            dotnet_diagnostic.CCA0001.severity = warning
            """));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_OutsideBaseHierarchy()
    {
        // arrange
        var source = WithLib(
            """
            using Lib;

            class Unrelated
            {
                [|Derived|].Nested item = null!;
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_SimplifyNameDisabledInCompilationOptions()
    {
        // arrange
        var source = WithLib(
            """
            using Lib;

            class Sub : Base
            {
                {|#0:Derived|}.Nested item = null!;
            }
            """);

        var test = CreateTest(source);
        test.SolutionTransforms.Add(
            (solution, projectId) => solution.WithProjectCompilationOptions(
                projectId,
                solution.GetProject(projectId)!.CompilationOptions!.WithSpecificDiagnosticOptions(
                    new Dictionary<string, ReportDiagnostic>
                    {
                        ["CCA0001"] = ReportDiagnostic.Suppress
                    })));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Nested", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_SimplifyNameDisabledInEditorConfig()
    {
        // arrange
        var source = WithLib(
            """
            using Lib;

            class Sub : Base
            {
                {|#0:Derived|}.Nested item = null!;
            }
            """);

        var test = CreateTest(source);
        test.TestState.AnalyzerConfigFiles.Add((
            "/.editorconfig",
            """
            [*.cs]
            dotnet_diagnostic.CCA0001.severity = none
            """));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Nested", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_SimplifyNameDisabledInGlobalConfig()
    {
        // arrange
        var source = WithLib(
            """
            using Lib;

            class Sub : Base
            {
                {|#0:Derived|}.Nested item = null!;
            }
            """);

        var test = CreateTest(source);
        test.TestState.AnalyzerConfigFiles.Add((
            "/.globalconfig",
            """
            is_global = true
            dotnet_diagnostic.CCA0001.severity = none
            """));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Nested", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_SimplifyNameRemovesOnlyNamespace()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                object M()
                {
                    return {|#0:System.Text.UTF8Encoding|}.Default;
                }
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Default", "Encoding"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
