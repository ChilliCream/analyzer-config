namespace ChilliCream.Analyzers.Tests;

public sealed partial class SimplifyNameAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_EarlierLocalWithSameNameAndType()
    {
        // arrange
        const string source =
            """
            using Paint;

            namespace Paint
            {
                public sealed class Color
                {
                    public static readonly Color Red = new();
                }
            }

            namespace App
            {
                public sealed class Canvas
                {
                    public object M()
                    {
                        var Color = Paint.Color.Red;
                        var other = [|Paint.|]Color.Red;

                        return other;
                    }
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_ForEachVariableWithSameNameAndType()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                object M(Encoding[] encodings)
                {
                    object result = null;

                    foreach (var Encoding in encodings)
                    {
                        result = [|System.Text.|]Encoding.UTF8;
                    }

                    return result;
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_ParameterWithSameNameAndType()
    {
        // arrange
        const string source =
            """
            using Paint;

            namespace Paint
            {
                public sealed class Color
                {
                    public static readonly Color Red = new();
                }
            }

            namespace App
            {
                public sealed class Canvas
                {
                    public object M(Color Color) => [|Paint.|]Color.Red;
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_PropertyWithSameNameAndType()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                public Encoding Encoding { get; } = [|System.Text.|]Encoding.UTF8;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ColorColorMethodCall()
    {
        // arrange
        const string source =
            """
            using System;
            using Paint;

            namespace Paint
            {
                public sealed class Color
                {
                    public static string M(object value) => "static M(object)";

                    public string M(int value) => "instance M(int)";
                }
            }

            namespace App
            {
                public sealed class Canvas
                {
                    public Color Color { get; } = new();

                    public string Qualified() => Paint.Color.M(1);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ColorColorMethodGroup()
    {
        // arrange
        const string source =
            """
            using System;
            using Paint;

            namespace Paint
            {
                public sealed class Color
                {
                    public static string M(object value) => "static M(object)";

                    public string M(int value) => "instance M(int)";
                }
            }

            namespace App
            {
                public sealed class Canvas
                {
                    public Color Color { get; } = new();

                    public string Qualified()
                    {
                        Func<object, string> f = Paint.Color.M;

                        return f(1);
                    }
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ColorColorStaticExtensionProperty()
    {
        // arrange
        const string source =
            """
            using Paint;

            namespace Paint
            {
                public sealed class Color
                {
                }

                public static class ColorExtensions
                {
                    extension(Color)
                    {
                        public static Color Default => new();
                    }
                }
            }

            namespace App
            {
                public sealed class Canvas
                {
                    public Color Color { get; } = new();

                    public Color Qualified() => Paint.Color.Default;
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ContainingTypeLocalDeclaredLater()
    {
        // arrange
        const string source =
            """
            using Paint;

            namespace Paint
            {
                public sealed class Outer
                {
                    public sealed class Inner
                    {
                        public static readonly int Value = 1;
                    }
                }
            }

            namespace App
            {
                public static class Canvas
                {
                    public static int M()
                    {
                        var value = Paint.Outer.Inner.Value;
                        var Outer = new Outer();

                        return value + Outer.GetHashCode();
                    }
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_DeconstructionLocalInOwnAssignment()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                object M()
                {
                    var (Encoding, count) = (System.Text.Encoding.UTF8, 1);

                    return Encoding;
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_LocalDeclaredLater()
    {
        // arrange
        const string source =
            """
            using System.Text;

            namespace App
            {
                public static class LaterLocal
                {
                    public static int Count()
                    {
                        var length = System.Text.Encoding.UTF8.GetByteCount("x");
                        var Encoding = System.Text.Encoding.ASCII;

                        return length + Encoding.GetByteCount("y");
                    }
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
