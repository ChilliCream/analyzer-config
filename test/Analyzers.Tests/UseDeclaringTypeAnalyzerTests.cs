using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeAnalyzerTests
{
    private const string Lib =
        """
        namespace Lib
        {
            public class Base
            {
                public class Nested
                {
                    public class Deep;
                }
            }

            public class Derived : Base;
        }
        """;

    [Fact]
    public void SupportedDiagnostics_Should_HaveRuleMetadata_When_AnalyzerIsCreated()
    {
        // arrange
        var analyzer = new UseDeclaringTypeAnalyzer();

        // act
        var descriptor = Assert.Single(analyzer.SupportedDiagnostics);

        // assert
        Assert.Equal("CCA0003", descriptor.Id);
        Assert.Equal("Access members through their declaring type", descriptor.Title.ToString());
        Assert.Equal(
            "Access '{0}' through its declaring type '{1}'",
            descriptor.MessageFormat.ToString());
        Assert.Equal("Style", descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Info, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
        Assert.Empty(descriptor.CustomTags);
        Assert.Equal(
            "https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0003.md",
            descriptor.HelpLinkUri);
    }

    private static CSharpAnalyzerTest<UseDeclaringTypeAnalyzer, DefaultVerifier>
        CreateTest(string source)
    {
        return new CSharpAnalyzerTest<UseDeclaringTypeAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = source
        };
    }

    private static Task VerifyAsync(string source)
    {
        return CreateTest(source).RunAsync(TestContext.Current.CancellationToken);
    }

    private static string WithLib(string source)
    {
        return source + "\n\n" + Lib;
    }
}
