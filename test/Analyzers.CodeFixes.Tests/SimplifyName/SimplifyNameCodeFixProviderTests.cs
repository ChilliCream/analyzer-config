using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class SimplifyNameCodeFixProviderTests
{
    [Fact]
    public async Task Fix_Should_KeepComments_When_CommentsAroundName()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                /* a */ [|System.Text.|]StringBuilder /* b */ M() => new();
            }
            """;
        const string fixedSource =
            """
            using System.Text;

            class C
            {
                /* a */ StringBuilder /* b */ M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_ProduceSimpleName_When_AttributeQualifierAndSuffix()
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                [[|System.|]Obsolete[|Attribute|]]
                void M()
                {
                }
            }
            """;
        const string fixedSource =
            """
            using System;

            class C
            {
                [Obsolete]
                void M()
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveGlobal_When_GlobalPrefix()
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
        const string fixedSource =
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
                    Vendor.Logs.Record M() => new();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveOuter_When_NestedTypeQualifier()
    {
        // arrange
        const string source =
            """
            class Outer
            {
                public class Inner
                {
                }

                [|Outer.|]Inner M() => new();
            }
            """;
        const string fixedSource =
            """
            class Outer
            {
                public class Inner
                {
                }

                Inner M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveQualifier_When_Cref()
    {
        // arrange
        const string source =
            """
            using System.IO;

            class C
            {
                /// <see cref="[|System.IO.|]File"/>
                void M()
                {
                }
            }
            """;
        const string fixedSource =
            """
            using System.IO;

            class C
            {
                /// <see cref="File"/>
                void M()
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveQualifier_When_ExpressionNamespace()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                object M() => [|System.Text.|]Encoding.UTF8;
            }
            """;
        const string fixedSource =
            """
            using System.Text;

            class C
            {
                object M() => Encoding.UTF8;
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_RemoveQualifier_When_TypePositionNamespace()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                [|System.Text.|]StringBuilder M() => new();
            }
            """;
        const string fixedSource =
            """
            using System.Text;

            class C
            {
                StringBuilder M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    private static CSharpCodeFixTest<
        SimplifyNameAnalyzer,
        SimplifyNameCodeFixProvider,
        DefaultVerifier> CreateTest(string source, string fixedSource)
    {
        return new CSharpCodeFixTest<
            SimplifyNameAnalyzer,
            SimplifyNameCodeFixProvider,
            DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = source,
            FixedCode = fixedSource
        };
    }

    private static Task VerifyAsync(string source, string fixedSource)
    {
        return CreateTest(source, fixedSource).RunAsync(TestContext.Current.CancellationToken);
    }
}
