using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveThisQualificationAnalyzerTests
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

            public event EventHandler? Changed;

            public int Count => _count;

            public T Echo<T>(T value) => value;

            public int Scale(int value) => value * 2;
        }

        public static class WidgetExtensions
        {
            public static int Twice(this Widget widget) => 2;

            extension(Widget widget)
            {
                public int Thrice => 3;
            }
        }
        """;

    [Fact]
    public void SupportedDiagnostics_Should_HaveRuleMetadata_When_AnalyzerIsCreated()
    {
        // arrange
        var analyzer = new RemoveThisQualificationAnalyzer();

        // act
        var descriptor = Assert.Single(analyzer.SupportedDiagnostics);

        // assert
        Assert.Equal("CCA0004", descriptor.Id);
        Assert.Equal("Remove 'this' qualification", descriptor.Title.ToString());
        Assert.Equal(
            "Access to '{0}' can be simplified by removing 'this.'",
            descriptor.MessageFormat.ToString());
        Assert.Equal("Style", descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Info, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
        var tag = Assert.Single(descriptor.CustomTags);
        Assert.Equal("Unnecessary", tag);
        Assert.Equal(
            "https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0004.md",
            descriptor.HelpLinkUri);
    }

    [Fact]
    public async Task Analyze_Should_ReportAll_When_NineRemovalsInStatement()
    {
        // arrange
        var source = WithWidget(
            """
            int M()
            {
                var sum = [|this.|]_count + [|this.|]_count + [|this.|]_count
                    + [|this.|]_count + [|this.|]_count + [|this.|]_count
                    + [|this.|]_count + [|this.|]_count + [|this.|]_count;
                return sum;
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportBoth_When_TwoRemovalsInStatement()
    {
        // arrange
        var source = WithWidget(
            "int M() { var sum = [|this.|]_count + [|this.|]Count; return sum; }");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_BaseAccess()
    {
        // arrange
        var source = WithWidget("public override string ToString() => base.ToString();");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ExtensionMethod()
    {
        // arrange
        var source = WithWidget("int M() => this.Twice();");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ExtensionProperty()
    {
        // arrange
        var source = WithWidget("int M() => this.Thrice;");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_Indexer()
    {
        // arrange
        var source = WithWidget(
            """
            int this[int i] => i;

            int M() => this[0];
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_Event()
    {
        // arrange
        var source = WithWidget(
            """
            void M(EventHandler h)
            {
                [|this.|]Changed += h;
                [|this.|]Changed?.Invoke(this, EventArgs.Empty);
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_Field()
    {
        // arrange
        var source = WithWidget("int M() => {|#0:this.|}_count;");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0004", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("_count"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_GenericMethod()
    {
        // arrange
        var source = WithWidget("int M() => [|this.|]Echo<int>(1);");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_InvokedMethod()
    {
        // arrange
        var source = WithWidget("int M() => [|this.|]Scale(1);");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_MethodGroup()
    {
        // arrange
        var source = WithWidget("Func<int, int> M() => [|this.|]Scale;");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_Property()
    {
        // arrange
        var source = WithWidget("int M() => [|this.|]Count;");

        // act & assert
        await VerifyAsync(source);
    }

    private static CSharpAnalyzerTest<RemoveThisQualificationAnalyzer, DefaultVerifier>
        CreateTest(string source)
    {
        return new CSharpAnalyzerTest<RemoveThisQualificationAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = source
        };
    }

    private static Task VerifyAsync(string source)
    {
        return CreateTest(source).RunAsync(TestContext.Current.CancellationToken);
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
