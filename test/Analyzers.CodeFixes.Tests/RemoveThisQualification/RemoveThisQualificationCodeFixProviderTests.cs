using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveThisQualificationCodeFixProviderTests
{
    private const string Widget =
        """
        using System;
        using System.Linq;
        using System.Linq.Expressions;

        public partial class Widget
        {
            public int _count;
            public int _other;
            public Func<int, int> _map = value => value;

            public int Count => _count;

            public T Echo<T>(T value) => value;

            public int Scale(int value) => value * 2;
        }
        """;

    [Fact]
    public async Task Fix_Should_Compile_When_ExpressionTree()
    {
        // arrange
        var source = WithWidget("Expression<Func<int>> M() => () => [|this.|]_count;");
        var fixedSource = WithWidget("Expression<Func<int>> M() => () => _count;");

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_JoinLines_When_DotStartsNextLine()
    {
        // arrange
        var source = WithWidget(
            """
            int M() => [|this
                .|]_count;
            """);
        var fixedSource = WithWidget("int M() => _count;");

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_KeepLeadingTrivia_When_AccessStartsWrappedLine()
    {
        // arrange
        var source = WithWidget(
            """
            int M() => Math.Max(
                [|this.|]_count,
                [|this.|]Count);
            """);
        var fixedSource = WithWidget(
            """
            int M() => Math.Max(
                _count,
                Count);
            """);

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_NotCaptureParameter_When_PrimaryConstructorParameterHasMemberName()
    {
        // arrange
        const string source =
            """
            public class Gadget(int count)
            {
                private readonly int count = count;

                public int M() => [|this.|]count;
            }
            """;
        const string fixedSource =
            """
            public class Gadget(int count)
            {
                private readonly int count = count;

                public int M() => count;
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveThis_When_Field()
    {
        // arrange
        var source = WithWidget("int M() => [|this.|]_count;");
        var fixedSource = WithWidget("int M() => _count;");

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveThis_When_GenericMethod()
    {
        // arrange
        var source = WithWidget("int M() => [|this.|]Echo<int>(1);");
        var fixedSource = WithWidget("int M() => Echo<int>(1);");

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    private static CSharpCodeFixTest<
        RemoveThisQualificationAnalyzer,
        RemoveThisQualificationCodeFixProvider,
        DefaultVerifier> CreateTest(string source, string fixedSource)
    {
        return new CSharpCodeFixTest<
            RemoveThisQualificationAnalyzer,
            RemoveThisQualificationCodeFixProvider,
            DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            CompilerDiagnostics = CompilerDiagnostics.Warnings,
            DisabledDiagnostics = { "CS1591" },
            TestCode = source,
            FixedCode = fixedSource
        };
    }

    private static Task VerifyAsync(string source, string fixedSource)
    {
        return CreateTest(source, fixedSource).RunAsync(TestContext.Current.CancellationToken);
    }

    private static string WithWidget(string members)
    {
        return
            $$"""
            {{Widget}}

            public partial class Widget
            {
            {{members}}
            }
            """;
    }
}
