namespace ChilliCream.Analyzers.Tests;

public sealed partial class SimplifyNameAnalyzerTests
{
    [Fact]
    public async Task
        Analyze_Should_ReportNamespaceAndOuter_When_QualifiedNestedTypeThroughUsingStatic()
    {
        // arrange
        const string source =
            """
            using static Company.Outer;

            namespace Company
            {
                public class Outer
                {
                    public class Inner
                    {
                    }
                }
            }

            class C
            {
                [|Company.Outer.|]Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_QualifiedNestedTypeWithNamespaceImport()
    {
        // arrange
        const string source =
            """
            using Company;

            namespace Company
            {
                public class Outer
                {
                    public class Inner
                    {
                    }
                }
            }

            class C
            {
                [|Company.|]Outer.Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ColorColorPropertyHasOtherTypeOfSameName()
    {
        // arrange
        const string source =
            """
            namespace Paint
            {
                public class Color
                {
                    public class Nested
                    {
                        public static int X;
                    }
                }
            }

            public class C
            {
                public Paint.Color Color { get; } = null!;

                public int M() => Paint.Color.Nested.X;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NameAfterOpenGenericName()
    {
        // arrange
        const string source =
            """
            class Outer<T>
            {
                public class Inner
                {
                }

                System.Type M() => typeof(Outer<>.Inner);
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NestedTypeContainerIsEarlierLocal()
    {
        // arrange
        const string source =
            """
            namespace Lib
            {
                public class Outer
                {
                    public class Inner
                    {
                        public static int Create() => 0;
                    }
                }
            }

            public class C
            {
                public int M()
                {
                    var Outer = new Lib.Outer();

                    return Lib.Outer.Inner.Create();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NestedTypeContainerIsParameter()
    {
        // arrange
        const string source =
            """
            namespace Lib
            {
                public class Outer
                {
                    public class Inner
                    {
                        public static int Create() => 0;
                    }
                }
            }

            public class C
            {
                public int M(Lib.Outer Outer) => Lib.Outer.Inner.Create();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NestedTypeContainerIsProperty()
    {
        // arrange
        const string source =
            """
            namespace Lib
            {
                public class Outer
                {
                    public class Inner
                    {
                        public static int Create() => 0;
                    }
                }
            }

            public class C
            {
                public Lib.Outer Outer { get; } = new();

                public int M() => Lib.Outer.Inner.Create();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NestedTypeContainerIsPropertyInNameof()
    {
        // arrange
        const string source =
            """
            namespace Lib
            {
                public class Outer
                {
                    public class Inner
                    {
                        public static int Create() => 0;
                    }
                }
            }

            public class C
            {
                public Lib.Outer Outer { get; } = new();

                public string M() => nameof(Lib.Outer.Inner);
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NestedTypeOfOtherConstruction()
    {
        // arrange
        const string source =
            """
            class Outer<T>
            {
                public class Inner
                {
                }

                Outer<int>.Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NestedTypeOutsideOuter()
    {
        // arrange
        const string source =
            """
            class Outer
            {
                public class Inner
                {
                }
            }

            class C
            {
                Outer.Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_StaticMemberAccessInsideOuter()
    {
        // arrange
        const string source =
            """
            class Outer
            {
                public static Outer Create() => new();

                Outer M() => Outer.Create();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuterAndArgumentNamespace_When_TypeArgumentInKeptPart()
    {
        // arrange
        const string source =
            """
            using System.Text;

            namespace Lib
            {
                public class Outer
                {
                    public class Inner<T>
                    {
                    }
                }
            }

            class Derived : Lib.Outer
            {
                [|Lib.Outer.|]Inner<[|System.Text.|]StringBuilder> M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportOuterAndNamespace_When_RemovedSpanContainsUnreportableTypeArgument()
    {
        // arrange
        const string source =
            """
            using Lib;

            namespace Lib
            {
                public class Outer<T>
                {
                    public class Inner
                    {
                    }
                }
            }

            namespace Other
            {
                public class Foo
                {
                }
            }

            class Derived : [|Lib.|]Outer<Other.Foo>
            {
                [|Lib.Outer<Other.Foo>.|]Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportOuterNamespaceOnly_When_RemovedSpanContainsAliasQualifiedTypeArgument()
    {
        // arrange
        const string source =
            """
            using Lib;
            using System.Text;

            namespace Lib
            {
                public class Outer<T>
                {
                    public class Inner
                    {
                    }
                }
            }

            class Derived : [|Lib.|]Outer<[|global::System.Text.|]StringBuilder>
            {
                [|Lib.|]Outer<[|global::System.Text.|]StringBuilder>.Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportOuterNamespaceOnly_When_RemovedSpanContainsNestedTypeArgument()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;
            using Lib;
            using System.Text;

            namespace Lib
            {
                public class Outer<T>
                {
                    public class Inner
                    {
                    }
                }
            }

            class Derived : [|Lib.|]Outer<List<[|System.Text.|]StringBuilder>>
            {
                [|Lib.|]Outer<List<[|System.Text.|]StringBuilder>>.Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuterNamespaceOnly_When_RemovedSpanContainsTypeArgument()
    {
        // arrange
        const string source =
            """
            using Lib;
            using System.Text;

            namespace Lib
            {
                public class Outer<T>
                {
                    public class Inner
                    {
                    }
                }
            }

            class Derived : [|Lib.|]Outer<[|System.Text.|]StringBuilder>
            {
                [|Lib.|]Outer<[|System.Text.|]StringBuilder>.Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportOuterNamespaceOnly_When_ReportableNameInUnreportableTypeArgument()
    {
        // arrange
        const string source =
            """
            using Lib;
            using System.Text;

            namespace Lib
            {
                public class Outer<T>
                {
                    public class Inner
                    {
                    }
                }
            }

            namespace Other
            {
                public class Foo<T>
                {
                }
            }

            class Derived : [|Lib.|]Outer<Other.Foo<[|System.Text.|]StringBuilder>>
            {
                [|Lib.|]Outer<Other.Foo<[|System.Text.|]StringBuilder>>.Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_NestedTypeInExpressionInsideOuter()
    {
        // arrange
        const string source =
            """
            class Outer
            {
                public class Inner
                {
                    public static Inner Create() => new();
                }

                Inner M() => [|Outer.|]Inner.Create();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_NestedTypeInsideDerivedType()
    {
        // arrange
        const string source =
            """
            class Outer
            {
                public class Inner
                {
                }
            }

            class Derived : Outer
            {
                [|Outer.|]Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_NestedTypeInsideOuter()
    {
        // arrange
        const string source =
            """
            class Outer
            {
                public class Inner
                {
                }

                [|Outer.|]Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportOuter_When_NestedTypeThroughUsingStatic()
    {
        // arrange
        const string source =
            """
            using static Outer;

            class Outer
            {
                public class Inner
                {
                }
            }

            class C
            {
                [|Outer.|]Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
