namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveRedundantTypeArgumentsAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportNothing_When_AbstractClassTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                abstract class Shape;

                void M(Shape value)
                {
                    Id<Shape>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ClassTypeParameterTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                static void M<T>(T value) where T : class
                {
                    Id<T>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ContainingTypeArgumentTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                interface IWidget;

                class Outer<T> { public sealed class Inner; }

                void M(Outer<IWidget>.Inner value)
                {
                    Id<Outer<IWidget>.Inner>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_DynamicTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M(dynamic value)
                {
                    Id<dynamic>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_InterfaceArrayTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                interface IWidget;

                void M(IWidget[] value)
                {
                    Id<IWidget[]>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_InterfaceInLongTupleTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                interface IWidget;

                void M((int, int, int, int, int, int, int, IWidget) value)
                {
                    Id<(int, int, int, int, int, int, int, IWidget)>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_InterfaceTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                interface IWidget;

                void M(IWidget value)
                {
                    Id<IWidget>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NestedDynamicTypeArgument()
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                void M(Func<dynamic> value)
                {
                    Id<Func<dynamic>>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NestedInterfaceTypeArgument()
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                interface IWidget;

                void M(Func<IWidget> value)
                {
                    Id<Func<IWidget>>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_ObjectTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M(object value)
                {
                    Id<object>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_TupleWithInterfaceTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                interface IWidget;

                void M((string, IWidget) value)
                {
                    Id<(string, IWidget)>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_UnconstrainedTypeParameterTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                static void M<T>(T value)
                {
                    Id<T>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_UnsealedClassTypeArgument()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;

            class C
            {
                static T Id<T>(T value) => value;

                void M(List<int> value)
                {
                    Id<List<int>>(value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_ArrayTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M(string[] value)
                {
                    Id[|<string[]>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_ContainingTypeHasInterfaceTypeArgument()
    {
        // arrange
        const string source =
            """
            static class Factory<T>
            {
                public static U Make<U>(U value) => value;
            }

            interface IWidget;

            class C
            {
                void M()
                {
                    Factory<IWidget>.Make[|<int>|](1);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_DelegateTypeArgument()
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                void M(Func<string, int> value)
                {
                    Id[|<Func<string, int>>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_EnumTypeArgument()
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                void M(DayOfWeek value)
                {
                    Id[|<DayOfWeek>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_GenericStructTypeArgument()
    {
        // arrange
        const string source =
            """
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M(ValueTask<int> value)
                {
                    Id[|<ValueTask<int>>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_MultidimensionalArrayTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M(int[,] value)
                {
                    Id[|<int[,]>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_NamedTupleTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M((string Name, int Age) value)
                {
                    Id[|<(string Name, int Age)>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_NullableValueTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M(int? value)
                {
                    Id[|<int?>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_SealedClassTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                sealed class Widget;

                void M(Widget value)
                {
                    Id[|<Widget>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_SealedRecordTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                sealed record Widget;

                void M(Widget value)
                {
                    Id[|<Widget>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_StructTypeParameterTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                static void M<T>(T value) where T : struct
                {
                    Id[|<T>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_UnmanagedTypeParameterTypeArgument()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                static void M<T>(T value) where T : unmanaged
                {
                    Id[|<T>|](value);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
