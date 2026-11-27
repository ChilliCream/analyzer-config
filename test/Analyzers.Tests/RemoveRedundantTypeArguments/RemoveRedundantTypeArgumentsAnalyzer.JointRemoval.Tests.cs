using Microsoft.CodeAnalysis;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveRedundantTypeArgumentsAnalyzerTests
{
    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_NestedCallsTogetherPinOverload(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                static T Run<T>(Func<T> factory) => factory();

                static void Use(Func<int, int> map)
                {
                }

                static void Use(Func<string, string> map)
                {
                }

                void M()
                {
                    Use(x => Run<int>(() => Id<int>(x)));
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_RootHasMoreThanEightCandidates()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var sum = Id<int>(1)
                        + Id<int>(2)
                        + Id<int>(3)
                        + Id<int>(4)
                        + Id<int>(5)
                        + Id<int>(6)
                        + Id<int>(7)
                        + Id<int>(8)
                        + Id<int>(9);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_SiblingLambdasTogetherPinOverload(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                static void Use(Func<int, int> first, Func<int, int> second)
                {
                }

                static void Use(Func<string, string> first, Func<string, string> second)
                {
                }

                void M()
                {
                    Use(x => Id<int>(x), y => Id<int>(y));
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
        Analyze_Should_ReportNothing_When_TwoCallsInBlockLambdaTogetherPinOverload(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Id<T>(T value) => value;

                static void Use(Func<int, int> map)
                {
                }

                static void Use(Func<string, string> map)
                {
                }

                void M()
                {
                    Use(x => { Id<int>(x); return Id<int>(x); });
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_RootHasEightCandidates()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var sum = Id[|<int>|](1)
                        + Id[|<int>|](2)
                        + Id[|<int>|](3)
                        + Id[|<int>|](4)
                        + Id[|<int>|](5)
                        + Id[|<int>|](6)
                        + Id[|<int>|](7)
                        + Id[|<int>|](8);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
