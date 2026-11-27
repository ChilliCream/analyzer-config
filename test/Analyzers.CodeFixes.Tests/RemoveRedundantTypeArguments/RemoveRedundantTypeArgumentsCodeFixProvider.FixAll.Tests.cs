namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveRedundantTypeArgumentsCodeFixProviderTests
{
    [Fact]
    public async Task FixAll_Should_RemoveBoth_When_ChainedCalls()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> numbers)
                {
                    var first = numbers.Select[|<int, int>|](x => x).First[|<int>|]();
                }
            }
            """;
        const string fixedSource =
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int> numbers)
                {
                    var first = numbers.Select(x => x).First();
                }
            }
            """;
        var test = CreateTest(source, fixedSource);
        test.NumberOfFixAllIterations = 1;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FixAll_Should_RemoveBoth_When_NestedCalls()
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Run<T>(Func<T> f) => f();

                void M()
                {
                    var x = Run[|<int>|](() => Run[|<int>|](() => 1));
                }
            }
            """;
        const string fixedSource =
            """
            using System;

            class C
            {
                static T Run<T>(Func<T> f) => f();

                void M()
                {
                    var x = Run(() => Run(() => 1));
                }
            }
            """;
        var test = CreateTest(source, fixedSource);
        test.NumberOfFixAllIterations = 1;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FixAll_Should_RemoveBoth_When_SiblingCalls()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                static T Pick<T>(T first, T second) => first;

                void M()
                {
                    var x = Pick[|<long>|](Id[|<long>|](1L), 2L);
                }
            }
            """;
        const string fixedSource =
            """
            class C
            {
                static T Id<T>(T value) => value;

                static T Pick<T>(T first, T second) => first;

                void M()
                {
                    var x = Pick(Id(1L), 2L);
                }
            }
            """;
        var test = CreateTest(source, fixedSource);
        test.NumberOfFixAllIterations = 1;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
