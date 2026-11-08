namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveRedundantTypeArgumentsAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportNothing_When_OneOfTwoTypeParametersInNoParameter()
    {
        // arrange
        const string source =
            """
            class C
            {
                static TOut Convert<TIn, TOut>(TIn value) where TOut : struct => default;

                void M()
                {
                    Convert<int, long>(1);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_TypeParameterInNoParameter()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Create<T>() where T : struct => default;

                void M()
                {
                    Create<int>();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_TypeParameterOnlyInReturnType()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Parse<T>(string text) where T : struct => default;

                void M()
                {
                    Parse<int>("1");
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_TypeParameterInArrayParameter()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T First<T>(T[] items) => items[0];

                void M(int[] items)
                {
                    First[|<int>|](items);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_TypeParameterInGenericParameter()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;

            class C
            {
                static T Single<T>(List<T> items) => items[0];

                void M(List<int> numbers)
                {
                    Single[|<int>|](numbers);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
