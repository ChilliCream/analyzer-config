namespace ChilliCream.Analyzers.Tests;

public sealed partial class SimplifyNameAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_CrefConversionOperatorType()
    {
        // arrange
        const string source =
            """
            using System;

            struct Widget
            {
                /// <see cref="explicit operator [|System.|]Int32(Widget)"/>
                public static explicit operator int(Widget widget) => 0;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_CrefGenericType()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;

            class C
            {
                /// <see cref="[|System.Collections.Generic.|]List{T}"/>
                void M()
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_CrefMemberContainer()
    {
        // arrange
        const string source =
            """
            using System.IO;

            class C
            {
                /// <see cref="[|System.IO.|]File.Exists(string)"/>
                void M()
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_CrefParameterType()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                /// <see cref="M([|System.Text.|]StringBuilder)"/>
                void M(StringBuilder builder)
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_CrefType()
    {
        // arrange
        const string source =
            """
            using System.IO;

            class C
            {
                /// <see cref="[|System.IO.|]File"/>
                void M()
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespaces_When_CrefMemberContainerAndParameterType()
    {
        // arrange
        const string source =
            """
            using System.IO;

            class C
            {
                /// <see cref="[|System.IO.|]File.Open(string, [|System.IO.|]FileMode)"/>
                void M()
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespaces_When_CrefParameterTypeArgument()
    {
        // arrange
        const string source =
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                /// <see cref="M([|System.Collections.Generic.|]List{[|System.|]String})"/>
                void M(List<string> values)
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_CrefHasSyntaxWarning()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;

            class C
            {
                /// <see cref="System.Collections.Generic.List{int}"/>
                void M()
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_CrefMemberTypeQualifier()
    {
        // arrange
        const string source =
            """
            using System.IO;

            class C
            {
                /// <see cref="File.Exists(string)"/>
                void M()
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_CrefNamespace()
    {
        // arrange
        const string source =
            """
            class C
            {
                /// <see cref="System.IO"/>
                void M()
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_CrefSimpleNameBindsMember()
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
            }

            namespace App
            {
                public sealed class Canvas
                {
                    public Color Color { get; }

                    /// <see cref="Paint.Color"/>
                    public void M()
                    {
                    }
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_CrefSimpleNameBindsOtherType()
    {
        // arrange
        const string source =
            """
            using System.IO;

            class C
            {
                /// <see cref="System.IO.File"/>
                void M()
                {
                }

                class File
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_CrefNestedTypeInsideOuter()
    {
        // arrange
        const string source =
            """
            class Outer
            {
                /// <see cref="[|Outer.|]Inner"/>
                void M()
                {
                }

                public class Inner
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_CrefGlobalQualifiedType()
    {
        // arrange
        const string source =
            """
            using System.IO;

            class C
            {
                /// <see cref="[|global::System.IO.|]File"/>
                void M()
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
