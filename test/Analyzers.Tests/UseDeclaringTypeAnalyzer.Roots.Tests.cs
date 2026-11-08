using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_ConstructorInitializer()
    {
        // arrange
        var source = WrapRoot(
            """
            class Holder
            {
                public Holder(Base b) { }
            }

            class Sub : Holder
            {
                public Sub() : base([|Derived|].Create()) { }
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_ExpressionBodiedMethod()
    {
        // arrange
        var source = WrapRoot(
            """
            class C
            {
                object M() => [|Derived|].Create();
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_FieldInitializer()
    {
        // arrange
        var source = WrapRoot(
            """
            class C
            {
                object _field = [|Derived|].Create();
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_LocalFunctionExpressionBody()
    {
        // arrange
        var source = WrapRoot(
            """
            class C
            {
                void M()
                {
                    object Local() => [|Derived|].Create();
                    Local();
                }
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_PrimaryConstructorBase()
    {
        // arrange
        var source = WrapRoot(
            """
            class Holder(Base b);

            class Sub(Base b) : Holder([|Derived|].Create());
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_PropertyInitializer()
    {
        // arrange
        var source = WrapRoot(
            """
            class C
            {
                object Property { get; } = [|Derived|].Create();
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_Statement()
    {
        // arrange
        var source = WrapRoot(
            """
            class C
            {
                void M()
                {
                    [|Derived|].Create();
                }
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_TopLevelStatement()
    {
        // arrange
        var source = WrapRoot("[|Derived|].Create();");
        var test = CreateTest(source);
        test.SolutionTransforms.Add(
            (solution, projectId) => solution.WithProjectCompilationOptions(
                projectId,
                ((CSharpCompilationOptions)solution.GetProject(projectId)!.CompilationOptions!)
                    .WithOutputKind(OutputKind.ConsoleApplication)));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    private static string WrapRoot(string code)
    {
        return $$"""
            using Lib;

            {{code}}

            {{ExpressionLib}}
            """;
    }
}
