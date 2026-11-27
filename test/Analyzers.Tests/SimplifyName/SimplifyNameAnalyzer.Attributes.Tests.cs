namespace ChilliCream.Analyzers.Tests;

public sealed partial class SimplifyNameAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportNothing_When_EscapedAttributeSuffix()
    {
        // arrange
        // cspell:ignore ttribute
        const string source =
            """
            using System;

            [Obsolete\u0041ttribute]
            class C
            {
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_KeywordRemainder()
    {
        // arrange
        const string source =
            """
            using System;

            class classAttribute : Attribute
            {
            }

            [classAttribute]
            class C
            {
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_QualifierRemovalBindsOtherAttribute()
    {
        // arrange
        const string source =
            """
            using System;
            using Mine;

            namespace Other
            {
                public class MarkerAttribute : Attribute
                {
                }
            }

            namespace Mine
            {
                public class MarkerAttribute : Attribute
                {
                }
            }

            [Other.Marker]
            class C
            {
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_SuffixRemovalAmbiguous()
    {
        // arrange
        const string source =
            """
            using System;

            class Marker : Attribute
            {
            }

            class MarkerAttribute : Attribute
            {
            }

            [MarkerAttribute]
            class C
            {
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifierAndSuffix_When_GenericAttribute()
    {
        // arrange
        const string source =
            """
            using System;
            using Company;

            namespace Company
            {
                public class MarkerAttribute<T> : Attribute
                {
                }
            }

            [[|Company.|]Marker[|Attribute|]<int>]
            class C
            {
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifierAndSuffix_When_QualifiedAttributeWithSuffix()
    {
        // arrange
        const string source =
            """
            using System;

            [[|System.|]Obsolete[|Attribute|]]
            class C
            {
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifierOnly_When_CombinedRemovalAmbiguous()
    {
        // arrange
        const string source =
            """
            using System;

            namespace Lib
            {
                public class ThingAttribute : Attribute
                {
                }
            }

            namespace App
            {
                using Lib;

                public class Thing : Attribute
                {
                }

                [[|Lib.|]ThingAttribute]
                class C
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_AssemblyAttribute()
    {
        // arrange
        const string source =
            """
            using System.Reflection;

            [assembly: [|System.Reflection.|]AssemblyMetadata("a", "b")]
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_GlobalQualifiedAttribute()
    {
        // arrange
        const string source =
            """
            using System;

            [[|global::System.|]Obsolete]
            class C
            {
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_ModuleAttribute()
    {
        // arrange
        const string source =
            """
            using System.Diagnostics.CodeAnalysis;

            [module: [|System.Diagnostics.CodeAnalysis.|]SuppressMessage("Category", "Id")]
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_QualifiedAttribute()
    {
        // arrange
        const string source =
            """
            using System;

            [[|System.|]Obsolete]
            class C
            {
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_TypeQualifiedAttribute()
    {
        // arrange
        const string source =
            """
            using System;

            class Outer
            {
                public class MarkerAttribute : Attribute
                {
                }

                [[|Outer.|]Marker]
                class C
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportSuffix_When_AttributeSuffix()
    {
        // arrange
        const string source =
            """
            using System;

            [Obsolete[|Attribute|]]
            class C
            {
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
