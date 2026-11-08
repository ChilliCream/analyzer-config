using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveRedundantTypeArgumentsAnalyzerTests
{
    [Fact]
    public void SupportedDiagnostics_Should_HaveRuleMetadata_When_AnalyzerIsCreated()
    {
        // arrange
        var analyzer = new RemoveRedundantTypeArgumentsAnalyzer();

        // act
        var descriptor = Assert.Single(analyzer.SupportedDiagnostics);

        // assert
        Assert.Equal("CCA0002", descriptor.Id);
        Assert.Equal("Remove redundant type arguments", descriptor.Title.ToString());
        Assert.Equal(
            "Call can be simplified by removing '{0}'",
            descriptor.MessageFormat.ToString());
        Assert.Equal("Style", descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Info, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
        Assert.Equal(WellKnownDiagnosticTags.Unnecessary, Assert.Single(descriptor.CustomTags));
        Assert.Equal(
            "https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0002.md",
            descriptor.HelpLinkUri);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_InferenceGivesOtherType()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var wide = Id<long>(42);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NonGenericOverloadMatches()
    {
        // arrange
        const string source =
            """
            class C
            {
                static string M(int value) => "";

                static string M<T>(T value) => "";

                void Use()
                {
                    var text = M<int>(1);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_TypeArgumentsCannotBeInferred()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Id<T>(T value) => value;

                void M()
                {
                    var items = Id<string[]>(["x"]);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_ConditionalAccess()
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

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_ExtensionMethodCall()
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

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_InferenceGivesSameType()
    {
        // arrange
        const string source =
            """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    var task = Task.FromResult{|#0:<string>|}("x");
                }
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0002", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("<string>"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_LambdaReturnInfersTypeArgument()
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
                    var texts = numbers.Select[|<int, string>|](x => x.ToString());
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_LocalFunctionCall()
    {
        // arrange
        const string source =
            """
            class C
            {
                void M()
                {
                    static T Local<T>(T value) => value;

                    Local[|<int>|](1);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_StaticExtensionMethodCall()
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
                    var first = Enumerable.First[|<int>|](numbers);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportTypeArgumentList_When_TwoArgumentsInferSameType()
    {
        // arrange
        const string source =
            """
            class C
            {
                static T Pick<T>(T first, T second) => first;

                void M(string s)
                {
                    var picked = Pick[|<string>|](s, s);
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    private static CSharpAnalyzerTest<RemoveRedundantTypeArgumentsAnalyzer, DefaultVerifier>
        CreateTest(string source)
    {
        return new CSharpAnalyzerTest<RemoveRedundantTypeArgumentsAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = source
        };
    }

    private static CSharpAnalyzerTest<RemoveRedundantTypeArgumentsAnalyzer, DefaultVerifier>
        CreateTest(string source, NullableContextOptions nullable)
    {
        var test = CreateTest(source);
        test.SolutionTransforms.Add(
            (solution, projectId) => solution.WithProjectCompilationOptions(
                projectId,
                ((CSharpCompilationOptions)solution.GetProject(projectId)!.CompilationOptions!)
                    .WithNullableContextOptions(nullable)));

        return test;
    }

    private static Task VerifyAsync(string source)
    {
        return CreateTest(source).RunAsync(TestContext.Current.CancellationToken);
    }

    private static Task VerifyAsync(string source, NullableContextOptions nullable)
    {
        return CreateTest(source, nullable).RunAsync(TestContext.Current.CancellationToken);
    }
}
