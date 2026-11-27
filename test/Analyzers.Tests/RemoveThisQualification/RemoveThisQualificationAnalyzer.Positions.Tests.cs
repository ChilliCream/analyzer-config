namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveThisQualificationAnalyzerTests
{
    [Fact]
    public async Task
        Analyze_Should_ReportNothing_When_CollectionElementMakesEnclosingCallAmbiguous()
    {
        // arrange
        const string source =
            """
            using System;

            public class Picker
            {
                private int Get() => 1;

                private static int Get(int value) => value;

                private static string Take(Func<int>[] factories) => "factory";

                private static string Take(Func<int, int>[] maps) => "map";

                public string M() => Take([this.Get]);
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NoSpeculationRoot()
    {
        // arrange
        var source = WithWidget(
            """
            int M(int n)
            {
                switch (n)
                {
                    case 1 when this._count > 0:
                        return 1;
                }

                try
                {
                    return 0;
                }
                catch (Exception) when (this._count > 0)
                {
                    return 2;
                }
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_QueryWhereClause()
    {
        // arrange
        var source = WithWidget(
            """
            int[] M(int[] items)
                => (from i in items where i > this._count select i).ToArray();
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOnce_When_AttributedAccessorProperty()
    {
        // arrange
        var source = WithWidget(
            """
            [Obsolete]
            int Value
            {
                get { return [|this.|]_count; }
                set { [|this.|]_count = value; }
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOnce_When_AttributedExpressionBodiedProperty()
    {
        // arrange
        var source = WithWidget("[Obsolete] int Value => [|this.|]_count;");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOnce_When_PrimaryConstructorHasBaseArguments()
    {
        // arrange
        const string source =
            """
            public class Base(int value);

            public class Derived(int value) : Base(value)
            {
                private int _count = value;

                public int M() => [|this.|]_count;
            }

            public class Defaulted(int value = 0)
            {
                private int _count = value;

                public int M() => [|this.|]_count;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_Accessors()
    {
        // arrange
        var source = WithWidget(
            "int Value { get => [|this.|]_count; set => [|this.|]_count = value; }");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_AnonymousTypeMember()
    {
        // arrange
        var source = WithWidget("object M() => new { [|this.|]Count };");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_CollectionExpressionElement()
    {
        // arrange
        var source = WithWidget("int[] M() => [[|this.|]_count, [|this.|]_other];");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_Constructor()
    {
        // arrange
        var source = WithWidget("public Widget(int start) { [|this.|]_count = start; }");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_DeconstructionTarget()
    {
        // arrange
        var source = WithWidget(
            "void M() { ([|this.|]_count, [|this.|]_other) = (1, 2); }");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_ExpressionTree()
    {
        // arrange
        var source = WithWidget("Expression<Func<int>> M() => () => [|this.|]_count;");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_InitializerValue()
    {
        // arrange
        var source = WithWidget("Widget M() => new() { _other = [|this.|]_count };");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_Interpolation()
    {
        // arrange
        var source = WithWidget("string M() => $\"{[|this.|]_count}\";");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_LocalFunction()
    {
        // arrange
        var source = WithWidget(
            """
            int M()
            {
                return Local();

                int Local() => [|this.|]_count;
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_Nameof()
    {
        // arrange
        var source = WithWidget("string M() => nameof([|this.|]_count);");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_NestedLambda()
    {
        // arrange
        var source = WithWidget(
            "Func<int, Func<int>> M() => a => () => [|this.|]_count + a;");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_RefAndOut()
    {
        // arrange
        var source = WithWidget(
            """
            ref int M(string s)
            {
                int.TryParse(s, out [|this.|]_other);
                return ref [|this.|]_count;
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_SpreadElement()
    {
        // arrange
        var source = WithWidget(
            """
            private int[] _items = [1];

            int[] M() => [.. [|this.|]_items];
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_Statement()
    {
        // arrange
        var source = WithWidget("void M() { _other = [|this.|]_count; }");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_StructMember()
    {
        // arrange
        const string source =
            """
            struct Point
            {
                private int _x;

                public int X => [|this.|]_x;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_SwitchExpressionArmWhenClause()
    {
        // arrange
        var source = WithWidget(
            "int M(object o) => o switch { 1 when [|this.|]_count > 0 => 1, _ => 0 };");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_TupleElement()
    {
        // arrange
        var source = WithWidget(
            "(int, int) M() => ([|this.|]_count, [|this.|]_other);");

        // act & assert
        await VerifyAsync(source);
    }
}
