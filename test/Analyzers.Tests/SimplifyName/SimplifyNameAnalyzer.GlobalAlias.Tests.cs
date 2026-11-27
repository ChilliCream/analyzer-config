namespace ChilliCream.Analyzers.Tests;

public sealed partial class SimplifyNameAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportGlobal_When_GlobalPrefixOnly()
    {
        // arrange
        const string source =
            """
            namespace Vendor.Logs
            {
                public class Record;
            }

            namespace App
            {
                public class Record;

                class C
                {
                    [|global::|]Vendor.Logs.Record M() => new();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportGlobal_When_GlobalPrefixOnlyInExpression()
    {
        // arrange
        const string source =
            """
            namespace Vendor.Logs
            {
                public class Record
                {
                    public static Record Create() => new();
                }
            }

            namespace App
            {
                public class Record;

                class C
                {
                    object M() => [|global::|]Vendor.Logs.Record.Create();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_CastWouldBecomeBinaryExpression()
    {
        // arrange
        const string source =
            """
            public struct Widget
            {
                public static implicit operator Widget(int value) => default;
            }

            namespace App
            {
                class C
                {
                    object M(int value) => (global::Widget)+value;
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_GlobalPrefixNeededForRootNamespace()
    {
        // arrange
        const string source =
            """
            namespace Vendor.Logs
            {
                public class Record;
            }

            namespace App.Vendor
            {
            }

            namespace App
            {
                class C
                {
                    global::Vendor.Logs.Record M() => new();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
