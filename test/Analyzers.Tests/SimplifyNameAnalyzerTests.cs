using Microsoft.CodeAnalysis;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class SimplifyNameAnalyzerTests
{
    [Fact]
    public void SupportedDiagnostics_Should_HaveRuleMetadata_When_AnalyzerIsCreated()
    {
        // arrange
        var analyzer = new SimplifyNameAnalyzer();

        // act
        var descriptor = Assert.Single(analyzer.SupportedDiagnostics);

        // assert
        Assert.Equal("CCA0001", descriptor.Id);
        Assert.Equal("Simplify name", descriptor.Title.ToString());
        Assert.Equal(
            "Name can be simplified by removing '{0}'",
            descriptor.MessageFormat.ToString());
        Assert.Equal("Style", descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Info, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
        Assert.Equal(WellKnownDiagnosticTags.Unnecessary, Assert.Single(descriptor.CustomTags));
        Assert.Equal(
            "https://github.com/ChilliCream/analyzer-config/blob/main/docs/rules/CCA0001.md",
            descriptor.HelpLinkUri);
    }
}
