using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveRedundantTypeArgumentsCodeFixProviderTests
{
    [Fact]
    public async Task Fix_Should_KeepSpace_When_SpaceBeforeArgumentList()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var x = Id[|<int>|] (1);
                }
            }
            """;
        const string fixedSource =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var x = Id (1);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_OfferRemoveTypeArgumentsAction_When_TypeArgumentsAreRedundant()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var x = Id[|<int>|](1);
                }
            }
            """;
        const string fixedSource =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var x = Id(1);
                }
            }
            """;
        var test = CreateTest(source, fixedSource);
        test.CodeActionEquivalenceKey = "CCA0002";
        test.CodeActionVerifier =
            (action, verifier) => verifier.Equal("Remove type arguments", action.Title);

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Fix_Should_RemoveTypeArguments_When_ConditionalAccess()
    {
        // arrange
        const string source =
            """
            #nullable enable
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int>? numbers)
                {
                    var first = numbers?.First[|<int>|]();
                }
            }
            """;
        const string fixedSource =
            """
            #nullable enable
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(List<int>? numbers)
                {
                    var first = numbers?.First();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveTypeArguments_When_ExtensionMethodCall()
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
                    var first = numbers.First[|<int>|]();
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
                    var first = numbers.First();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveTypeArguments_When_MemberAccessCall()
    {
        // arrange
        const string source =
            """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    var task = Task.FromResult[|<string>|]("x");
                }
            }
            """;
        const string fixedSource =
            """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    var task = Task.FromResult("x");
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveTypeArguments_When_SimpleNameCall()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var x = Id[|<int>|](1);
                }
            }
            """;
        const string fixedSource =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var x = Id(1);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveWholeList_When_ListSpansLines()
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
                    var texts = numbers.Select[|<
                        int,
                        string>|](x => x.ToString());
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
                    var texts = numbers.Select(x => x.ToString());
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    private static CSharpCodeFixTest<
        RemoveRedundantTypeArgumentsAnalyzer,
        RemoveRedundantTypeArgumentsCodeFixProvider,
        DefaultVerifier> CreateTest(string source, string fixedSource)
    {
        return new CSharpCodeFixTest<
            RemoveRedundantTypeArgumentsAnalyzer,
            RemoveRedundantTypeArgumentsCodeFixProvider,
            DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            CompilerDiagnostics = CompilerDiagnostics.Warnings,
            TestCode = source,
            FixedCode = fixedSource
        };
    }

    private static Task VerifyAsync(string source, string fixedSource)
    {
        return CreateTest(source, fixedSource).RunAsync(TestContext.Current.CancellationToken);
    }
}
