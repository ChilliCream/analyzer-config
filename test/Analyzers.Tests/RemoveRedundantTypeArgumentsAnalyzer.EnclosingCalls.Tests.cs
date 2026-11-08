using Microsoft.CodeAnalysis;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveRedundantTypeArgumentsAnalyzerTests
{
    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_CallInQueryWhereClause(NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            sealed class Widget
            {
                public Widget Where(Func<int, bool> predicate) => this;

                public Widget Where(Func<string, bool> predicate) => this;
            }

            class C
            {
                static bool P<T>(T value) => true;

                void M(Widget widget)
                {
                    var query = from x in widget where P<int>(x) select x;
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalInBlockLambdaMakesEnclosingCallAmbiguous(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                static void Use(Func<string, string> map)
                {
                }

                static void Use(Func<int, int> map)
                {
                }

                static void Apply<T>(T seed, Func<T, T> map)
                {
                }

                void M()
                {
                    Use(x => { return Id<int>(x); });
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalMakesCollectionInitializerAddAmbiguous(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;
            using System.Collections;

            sealed class Bag : IEnumerable
            {
                public void Add(Func<int, int> map)
                {
                }

                public void Add(Func<string, string> map)
                {
                }

                public IEnumerator GetEnumerator() => throw new NotSupportedException();
            }

            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var bag = new Bag { x => Id<int>(x) };
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalMakesComplexElementInitializerAmbiguous(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;
            using System.Collections;

            sealed class Bag : IEnumerable
            {
                public void Add(int key, Func<int, int> map)
                {
                }

                public void Add(int key, Func<string, string> map)
                {
                }

                public IEnumerator GetEnumerator() => throw new NotSupportedException();
            }

            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var bag = new Bag { { 1, x => Id<int>(x) } };
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalMakesConstructorInitializerAmbiguous(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class B
            {
                protected B(Func<string, string> map)
                {
                }

                protected B(Func<int, int> map)
                {
                }
            }

            class D : B
            {
                D() : base(x => H.Id<int>(x))
                {
                }
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalMakesEnclosingCallAmbiguous(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                static void Use(Func<string, string> map)
                {
                }

                static void Use(Func<int, int> map)
                {
                }

                static void Apply<T>(T seed, Func<T, T> map)
                {
                }

                void M()
                {
                    Use(x => Id<int>(x));
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalMakesNestedCollectionInitializerAmbiguous(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;
            using System.Collections;

            sealed class Bag : IEnumerable
            {
                public void Add(Func<int, int> map)
                {
                }

                public void Add(Func<string, string> map)
                {
                }

                public IEnumerator GetEnumerator() => throw new NotSupportedException();
            }

            sealed class Outer
            {
                public Bag Items { get; } = new();
            }

            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var outer = new Outer { Items = { x => Id<int>(x) } };
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalMakesPrimaryConstructorBaseAmbiguous(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class B
            {
                protected B(Func<string, string> map)
                {
                }

                protected B(Func<int, int> map)
                {
                }
            }

            class E() : B(x => H.Id<int>(x));

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalMakesQuerySelectAmbiguous(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            sealed class Widget
            {
                public Widget Select(Func<int, int> selector) => this;

                public Widget Select(Func<string, string> selector) => this;
            }

            class C
            {
                static T Id<T>(T value) => value;

                void M(Widget widget)
                {
                    var query = from x in widget select Id<int>(x);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_BlockLambdaInEnclosingGenericCall(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                static void Use(Func<string, string> map)
                {
                }

                static void Use(Func<int, int> map)
                {
                }

                static void Apply<T>(T seed, Func<T, T> map)
                {
                }

                void M()
                {
                    Apply("x", x => { return Id[|<string>|](x); });
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_CollectionInitializerAddUnambiguous(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;
            using System.Collections;

            sealed class Bag : IEnumerable
            {
                public void Add(int value)
                {
                }

                public IEnumerator GetEnumerator() => throw new NotSupportedException();
            }

            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var bag = new Bag { Id[|<int>|](1) };
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_EnclosingGenericCallInfersSameType(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                static void Use(Func<string, string> map)
                {
                }

                static void Use(Func<int, int> map)
                {
                }

                static void Apply<T>(T seed, Func<T, T> map)
                {
                }

                void M()
                {
                    Apply("x", x => Id[|<string>|](x));
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }
}
