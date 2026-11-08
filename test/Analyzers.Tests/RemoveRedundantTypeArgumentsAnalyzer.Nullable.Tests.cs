namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveRedundantTypeArgumentsAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportNothing_When_AnnotationsOnlyInfersNullableType()
    {
        // arrange
        const string source =
            """
            #nullable enable annotations
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M(string? n)
                {
                    var x = Id<string>(n);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_AssignmentNarrowsArgument()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M(string? n)
                {
                    n = "x";

                    var x = Id<string?>(n);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_LambdaReturnsNonNullableType()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                static T Run<T>(Func<T> factory) => factory();

                void M(string s)
                {
                    var x = Run<string?>(() => s);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_LambdaReturnsNullableType()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                static T Run<T>(Func<T> factory) => factory();

                void M(string? n)
                {
                    var x = Run<string>(() => n);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NoNullableWarningsEnabled()
    {
        // arrange
        const string source =
            """
            #nullable enable annotations
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M(string s, string? n)
                {
                    var x = Id<string>(s);
                    var y = Id<string?>(n);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NonNullableTypeArgumentForNullableArgument()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M(string? n)
                {
                    var x = Task.FromResult<string>(n);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NullableTypeArgumentForNonNullableArgument()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M(string s)
                {
                    var x = Id<string?>(s);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_TupleElementNamesDiffer()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var x = Id<(string Name, int Age)>(("x", 1));
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_LambdaReturnsSameType()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                static T Run<T>(Func<T> factory) => factory();

                void M(string s)
                {
                    var x = Run[|<string>|](() => s);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_NestedAnnotationMatches()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M(KeyValuePair<string, string?> pair)
                {
                    var x = Id[|<KeyValuePair<string, string?>>|](pair);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_NullCheckNarrowsArgument()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M(string? n)
                {
                    if (n is null)
                    {
                        return;
                    }

                    var x = Id[|<string>|](n);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_NullableAnnotationsOnly()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                string? Name { get; set; }

            #nullable enable annotations
                void M(string? n)
                {
                    var x = Id[|<string?>|](n);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_NullableArgumentInfersNullableType()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M(string? n)
                {
                    var x = Id[|<string?>|](n);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_NullableDisabled()
    {
        // arrange
        const string source =
            """
            #nullable disable
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class C
            {
                static T Id<T>(T value) => value;

                void M(string s)
                {
                    var x = Id[|<string>|](s);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
