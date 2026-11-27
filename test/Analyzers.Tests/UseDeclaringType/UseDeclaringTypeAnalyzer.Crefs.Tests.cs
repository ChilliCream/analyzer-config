using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportNothing_When_CrefContainerQualifiesMember()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public class Nested
                {
                    public static void M()
                    {
                    }
                }
            }

            class Derived : Base;

            class C
            {
                /// <see cref="{|CS1574:Derived.Nested.M|}"/>
                void M()
                {
                }
            }
            """;

        var test = CreateTest(source);
        test.CompilerDiagnostics = CompilerDiagnostics.Warnings;
        test.SolutionTransforms.Add(
            (solution, projectId) => solution.WithProjectParseOptions(
                projectId,
                ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!)
                    .WithDocumentationMode(DocumentationMode.Diagnose)));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_CrefMemberReference()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public static void Create()
                {
                }
            }

            class Derived : Base;

            class C
            {
                /// <see cref="{|CS1574:Derived.Create|}"/>
                void M()
                {
                }
            }
            """;

        var test = CreateTest(source);
        test.CompilerDiagnostics = CompilerDiagnostics.Warnings;
        test.SolutionTransforms.Add(
            (solution, projectId) => solution.WithProjectParseOptions(
                projectId,
                ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!)
                    .WithDocumentationMode(DocumentationMode.Diagnose)));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_CrefParameterType()
    {
        // arrange
        var source = WithLib(
            """
            using Lib;

            class C
            {
                /// <see cref="Take({|#0:Derived|}.Nested)"/>
                public static void Take(Base.Nested n)
                {
                }
            }
            """);

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Nested", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
