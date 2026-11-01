namespace ChilliCream.Analyzers.Tests;

public sealed partial class SimplifyNameCodeFixProviderTests
{
    [Fact]
    public async Task FixAll_Should_ProduceSimpleName_When_QualifiedAttributeWithSuffix()
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

                [[|System.|]Obsolete[|Attribute|]]
                void N()
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

                [Obsolete]
                void N()
                {
                }
            }
            """;
        var test = CreateTest(source, fixedSource);
        test.NumberOfFixAllIterations = 1;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FixAll_Should_RemoveAllQualifiers_When_CodeAndCrefs()
    {
        // arrange
        const string source =
            """
            using System;
            using System.IO;
            using System.Text;

            class C
            {
                /// <see cref="[|System.IO.|]File"/>
                void M([|System.Text.|]StringBuilder b) => [|System.|]Console.WriteLine(b);
            }
            """;
        const string fixedSource =
            """
            using System;
            using System.IO;
            using System.Text;

            class C
            {
                /// <see cref="File"/>
                void M(StringBuilder b) => Console.WriteLine(b);
            }
            """;
        var test = CreateTest(source, fixedSource);
        test.NumberOfFixAllIterations = 1;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FixAll_Should_RemoveBothQualifiers_When_TypeArgumentInsideReportedName()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;
            using System.Text;

            class C
            {
                [|System.Collections.Generic.|]List<[|System.Text.|]StringBuilder> M() => new();
            }
            """;
        const string fixedSource =
            """
            using System.Collections.Generic;
            using System.Text;

            class C
            {
                List<StringBuilder> M() => new();
            }
            """;
        var test = CreateTest(source, fixedSource);
        test.NumberOfFixAllIterations = 1;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
