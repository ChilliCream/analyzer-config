namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportAll_When_RootHasEightCandidates()
    {
        // arrange
        var terms = string.Join(" + ", Enumerable.Repeat("[|Derived|].Value", 8));
        var source = WrapExpression($"var x = {terms};");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportBoth_When_RewritesAreSafeTogether()
    {
        // arrange
        var source = WrapExpression(
            "var x = Apply(1, n => [|Derived|].Id(n)) + [|Derived|].Value;",
            members: "static T Apply<T>(T seed, Func<T, T> map) => map(seed);");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_RewritesTogetherMakeCallAmbiguous()
    {
        // arrange
        const string source =
            """
            using System;

            class B
            {
                public static int M(int x) => x;
                public static int M(object o) => 0;
            }

            class D : B
            {
                public static string M(string s) => s;
            }

            static class P
            {
                static void Use(Func<int, int> f, Func<int, int> g) { }
                static void Use(Func<string, int> f, Func<string, int> g) { }

                static void Run()
                {
                    Use(x => D.M(x), y => D.M(y));
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_RootHasNineCandidates()
    {
        // arrange
        var terms = string.Join(" + ", Enumerable.Repeat("Derived.Value", 9));
        var source = WrapExpression($"var x = {terms};");

        // act & assert
        await VerifyAsync(source);
    }
}
