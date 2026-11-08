using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NameofGroupSpansDeclaringTypes()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public static void M(int x) { }
            }

            class Derived : Base
            {
                public static void M(string s) { }
            }

            class C
            {
                string Run() => nameof(Derived.M);
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NameofGroupSpansMiddleType()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public static void M(int x) { }
            }

            class Middle : Base
            {
                public static void M(string s) { }
            }

            class Derived : Middle;

            class C
            {
                string Run() => nameof(Derived.M);
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NameofInstanceMember()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public int Name { get; }
            }

            class Derived : Base;

            class C
            {
                string Run() => nameof(Derived.Name);
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_NestedQualifiersInNameof()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public static int Count;
            }

            class B
            {
                public class Inner : Base;
            }

            class D : B;

            class C
            {
                string Run() => nameof({|#0:D.Inner|}.Count);
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Count", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_NameofArgument()
    {
        // arrange
        var source = WrapExpression("var x = nameof({|#0:Derived|}.Create);");
        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Create", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
