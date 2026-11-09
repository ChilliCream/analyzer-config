using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportInner_When_OuterDeclaringTypeIsGeneric()
    {
        // arrange
        const string source =
            """
            class Base<T>
            {
                public static void M() { }
            }

            class B
            {
                public class Inner : Base<int>;
            }

            class D : B;

            class C
            {
                void Run()
                {
                    {|#0:D|}.Inner.M();
                }
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Inner", "B"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportInner_When_OuterRewriteChangesBinding()
    {
        // arrange
        const string source =
            """
            using System;

            class Base
            {
                public static int M(int x) => x;
                public static int M(object o) => 0;
            }

            class B
            {
                public class Inner : Base
                {
                    public static string M(string s) => s;
                }
            }

            class D : B;

            static class P
            {
                static void Use(Func<int, int> f) { }
                static void Use(Func<string, int> f) { }

                static void Run()
                {
                    Use(x => {|#0:D|}.Inner.M(x));
                }
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Inner", "B"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportInner_When_OuterRewritesFailTogether()
    {
        // arrange
        const string source =
            """
            using System;

            class Base
            {
                public static int M(int x) => x;
                public static int M(object o) => 0;
            }

            class B
            {
                public class Inner : Base
                {
                    public static string M(string s) => s;
                }
            }

            class D : B;

            static class P
            {
                static void Use(Func<int, int> f, Func<int, int> g) { }
                static void Use(Func<string, int> f, Func<string, int> g) { }

                static void Run()
                {
                    Use(x => {|#0:D|}.Inner.M(x), y => {|#1:D|}.Inner.M(y));
                }
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Inner", "B"));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(1)
                .WithArguments("Inner", "B"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_GenericQualifierContainsInnerInExpression()
    {
        // arrange
        const string source =
            """
            class B
            {
                public class Inner;
            }

            class D : B;

            class Outer<T>;

            class C
            {
                bool Run(object a, object b)
                {
                    return {|#0:Outer<D.Inner>|}.ReferenceEquals(a, b);
                }
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("ReferenceEquals", "object"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_GenericQualifierContainsInnerInNameof()
    {
        // arrange
        const string source =
            """
            class B
            {
                public class Inner;
            }

            class D : B;

            class Outer<T>;

            class C
            {
                string Run() => nameof({|#0:Outer<D.Inner>|}.ReferenceEquals);
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("ReferenceEquals", "object"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_GenericQualifierContainsInnerInTypeName()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public class Deep;
            }

            class B
            {
                public class Inner;
            }

            class D : B;

            class Outer<T> : Base;

            class C
            {
                void Run()
                {
                    {|#0:Outer<D.Inner>|}.Deep item = null!;
                }
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Deep", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_NestedQualifiers()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public static void M() { }
            }

            class B
            {
                public class Inner : Base;
            }

            class D : B;

            class C
            {
                void Run()
                {
                    {|#0:D.Inner|}.M();
                }
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("M", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_NestedQualifiersInTypeName()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public class Deep;
            }

            class B
            {
                public class Inner : Base;
            }

            class D : B;

            class C
            {
                void Run()
                {
                    {|#0:D.Inner|}.Deep item = null!;
                }
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Deep", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
