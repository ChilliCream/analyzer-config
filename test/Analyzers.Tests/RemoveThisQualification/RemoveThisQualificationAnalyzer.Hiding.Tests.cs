using Microsoft.CodeAnalysis.CSharp;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveThisQualificationAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportNothing_When_FieldInAccessor()
    {
        // arrange
        var source = WithWidget(
            """
            private int field;

            int Value { get => this.field; }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_FieldInAccessorUnderCSharp13()
    {
        // arrange
        const string source =
            """
            public class Holder
            {
                private int field;

                public int Value { get => this.field; }
            }
            """;

        var test = CreateTest(source);
        test.SolutionTransforms.Add(
            (solution, projectId) => solution.WithProjectParseOptions(
                projectId,
                ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!)
                    .WithLanguageVersion(LanguageVersion.CSharp13)));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_FieldInExpressionBodiedProperty()
    {
        // arrange
        var source = WithWidget(
            """
            private int field;

            int Value => this.field;
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportNothing_When_InheritedMemberShadowsPartialPrimaryConstructorParameter()
    {
        // arrange
        const string source =
            """
            public class Base
            {
                protected int count = 10;
            }

            public partial class Derived(int count) : Base
            {
                public int P = count;
            }

            public partial class Derived
            {
                public int M() => this.count;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportNothing_When_InheritedMemberShadowsPrimaryConstructorParameter()
    {
        // arrange
        const string source =
            """
            public class Base
            {
                protected int count = 10;
            }

            public class Derived(int count) : Base
            {
                public int P = count;

                public int M() => this.count;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_LambdaParameterHidesMember()
    {
        // arrange
        var source = WithWidget("Func<int, int> M() => _count => this._count + _count;");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_LocalDeclaredLaterInBlock()
    {
        // arrange
        var source = WithWidget(
            """
            int M()
            {
                var y = this._count;
                var _count = 1;
                return y + _count;
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_LocalFunctionHidesMethod()
    {
        // arrange
        var source = WithWidget(
            """
            int M()
            {
                return this.Scale(1);

                int Scale(int v) => v;
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_LocalHidesMember()
    {
        // arrange
        var source = WithWidget(
            """
            int M()
            {
                var _count = 1;
                return this._count + _count;
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ParameterHidesMember()
    {
        // arrange
        var source = WithWidget("public Widget(int _count) { this._count = _count; }");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_PatternVariableHidesMember()
    {
        // arrange
        var source = WithWidget(
            "bool M(object o) => this._count > 0 && o is int _count && _count > 1;");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_RangeVariableHidesMember()
    {
        // arrange
        var source = WithWidget(
            """
            int[] M(int[] items)
                => (from _count in items select this._count).ToArray();
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_StaticOverloadWinsWithoutThis()
    {
        // arrange
        const string source =
            """
            public class Picker
            {
                public int Pick(object value) => 1;

                public static int Pick(int value) => 2;

                public int M() => this.Pick(1);
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ValueInSetter()
    {
        // arrange
        var source = WithWidget(
            """
            private int value;

            int Value { set => this.value = value; }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_ColorColorProperty()
    {
        // arrange
        const string source =
            """
            public class Color
            {
                public string Name => "";
            }

            public class Paint
            {
                public Color Color { get; } = new();

                public string M() => [|this.|]Color.Name;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_EscapedFieldInAccessor()
    {
        // arrange
        var source = WithWidget(
            """
            private int field;

            int Value { get => [|this.|]@field; }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_FieldNamedFieldOutsideAccessor()
    {
        // arrange
        var source = WithWidget(
            """
            private int field;

            int M() => [|this.|]field;
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_PrimaryConstructorParameterHasMemberName()
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

        // act & assert
        await VerifyAsync(source);
    }
}
