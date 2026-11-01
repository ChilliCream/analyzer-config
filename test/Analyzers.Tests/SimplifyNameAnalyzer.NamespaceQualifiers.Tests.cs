using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class SimplifyNameAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_BeMessageArgument_When_RemovedText()
    {
        // arrange
        var test = CreateTest(
            """
            using System.Text;

            class C
            {
                {|#0:System.Text.|}StringBuilder M() => new();
            }
            """);
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<SimplifyNameAnalyzer, DefaultVerifier>
                .Diagnostic()
                .WithLocation(0)
                .WithArguments("System.Text."));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_KeepContainingType_When_NestedTypeOfGenericType()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;

            class C
            {
                int M(
                    [|System.Collections.Generic.|]Dictionary<string, int>.KeyCollection keys) =>
                    keys.Count;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportAlias_When_NamespaceAliasQualifier()
    {
        // arrange
        const string source =
            """
            using System.Text;
            using Text = System.Text;

            class C
            {
                [|Text.|]StringBuilder M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportInnerName_When_TypeArgument()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;
            using System.Text;

            class C
            {
                List<[|System.Text.|]StringBuilder> M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespaceOnce_When_GenericType()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;

            class C
            {
                [|System.Collections.Generic.|]List<int> M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_CastType()
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                int M(object value) => ([|System.|]Int32)value;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_GlobalUsingInOtherFile()
    {
        // arrange
        var test = CreateTest(
            """
            class C
            {
                [|System.Text.|]StringBuilder M() => new();
            }
            """);
        test.TestState.Sources.Add("global using System.Text;");

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_ObsoleteType()
    {
        // arrange
        const string source =
            """
            using System;
            using Lib;

            namespace Lib
            {
                [Obsolete]
                public class Old
                {
                }
            }

            class C
            {
                [|Lib.|]Old M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_ObsoleteTypeWarningIsError()
    {
        // arrange
        var test = CreateTest(
            """
            using System;
            using Lib;

            namespace Lib
            {
                [Obsolete]
                internal class Old
                {
                }
            }

            class C
            {
                {|CS0612:[|Lib.|]Old|} M() => new();
            }
            """);
        test.SolutionTransforms.Add(
            (solution, projectId) => solution.WithProjectCompilationOptions(
                projectId,
                solution
                    .GetProject(projectId)!
                    .CompilationOptions!
                    .WithGeneralDiagnosticOption(ReportDiagnostic.Error)));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_ParameterTypeWithGlobalUsing()
    {
        // arrange
        const string source =
            """
            global using System.Text;

            class C
            {
                void M([|System.Text.|]StringBuilder builder)
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_ParameterTypeWithUsingDirective()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                void M([|System.Text.|]StringBuilder builder)
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_TypeInEnclosingNamespace()
    {
        // arrange
        const string source =
            """
            namespace Company.Product
            {
                public class Widget
                {
                }
            }

            namespace Company.Product.Feature
            {
                class C
                {
                    [|Company.Product.|]Widget M() => new();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_TypeInLambda()
    {
        // arrange
        const string source =
            """
            using System;
            using System.Text;

            class C
            {
                void M()
                {
                    Func<object> f = () =>
                    {
                        [|System.Text.|]StringBuilder builder = new();

                        return builder;
                    };
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_TypeOfOpenGenericType()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;

            class C
            {
                object M() => typeof([|System.Collections.Generic.|]List<>);
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_TypeWithKeyword()
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                [|System.|]Int32 M() => 0;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNamespace_When_UsingInsideNamespaceBlock()
    {
        // arrange
        const string source =
            """
            namespace Features
            {
                using System.Text;

                class C
                {
                    [|System.Text.|]StringBuilder M() => new();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_AmbiguousSimpleName()
    {
        // arrange
        const string source =
            """
            using One;
            using Two;

            namespace One
            {
                public class Thing
                {
                }
            }

            namespace Two
            {
                public class Thing
                {
                }
            }

            class C
            {
                void M(One.Thing a, Two.Thing b)
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_CommentInRemovedSpan()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                System./* note */Text.StringBuilder M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_InaccessibleType()
    {
        // arrange
        const string source =
            """
            using Lib;

            namespace Lib
            {
                public class Outer
                {
                    private class Hidden
                    {
                    }
                }
            }

            class C
            {
                void M()
                {
                    Lib.Outer.{|CS0122:Hidden|} value = null;
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NamespaceNotImported()
    {
        // arrange
        const string source =
            """
            class C
            {
                System.Text.StringBuilder M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NestedTypeShadowsImport()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                void M(System.Text.StringBuilder builder)
                {
                }

                private sealed class StringBuilder
                {
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NonGlobalAliasQualifier()
    {
        // arrange
        const string source =
            """
            using Sys = System;
            using System.Text;

            class C
            {
                Sys::Text.StringBuilder M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_OnlyNamespaceRelativeFormBinds()
    {
        // arrange
        const string source =
            """
            namespace Company.Other
            {
                public class Tool
                {
                }
            }

            namespace Company.Product.Feature
            {
                public class Tool
                {
                }

                class C
                {
                    Company.Other.Tool M() => new();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_SimpleName()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                StringBuilder M(StringBuilder builder) => new StringBuilder();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_SimpleNameBindsTypeParameter()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C<StringBuilder>
            {
                System.Text.StringBuilder M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_TypeArgumentHasError()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;

            class C
            {
                System.Collections.Generic.List<{|CS0246:Missing|}> M() => new();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_TypeImportedOnlyThroughAlias()
    {
        // arrange
        const string source =
            """
            using Builders = System.Text;

            class C
            {
                Builders.Encoding E(Builders.Encoding value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_UnresolvedName()
    {
        // arrange
        const string source =
            """
            class C
            {
                System.Text.{|CS0234:Missing|} M() => null;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_GlobalQualifiedObjectCreation()
    {
        // arrange
        const string source =
            """
            using System.Text;

            class C
            {
                object M() => new [|global::System.Text.|]StringBuilder();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
