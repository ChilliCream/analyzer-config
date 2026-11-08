namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveRedundantTypeArgumentsAnalyzerTests
{
    [Fact]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalInBlockLambdaMakesEnclosingCallAmbiguous()
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
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalMakesConstructorInitializerAmbiguous()
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
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_RemovalMakesEnclosingCallAmbiguous()
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
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportNothing_When_RemovalMakesPrimaryConstructorBaseAmbiguous()
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
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_BlockLambdaInEnclosingGenericCall()
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
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_EnclosingGenericCallInfersSameType()
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
        await VerifyAsync(source);
    }
}
