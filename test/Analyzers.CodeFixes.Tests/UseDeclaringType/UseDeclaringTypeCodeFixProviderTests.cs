using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeCodeFixProviderTests
{
    private const string Lib =
        """
        namespace Lib
        {
            class Base
            {
                public static int Count => 0;
                public static int Value => 1;
                public static Base Create() => new();
                public static int Id(int value) => value;
                protected static int Protect() => 0;

                public class Nested;
            }

            class Derived : Base;
        }
        """;

    [Fact]
    public async Task Fix_Should_Compile_When_ProtectedMemberThroughSiblingType()
    {
        // arrange
        const string other =
            """
            class Other : Base
            {
                int M() => [|Derived|].Protect();
            }
            """;
        const string fixedOther =
            """
            class Other : Base
            {
                int M() => Base.Protect();
            }
            """;
        var source = Wrap(string.Empty, extra: other);
        var fixedSource = Wrap(string.Empty, extra: fixedOther);

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_KeepComment_When_CommentBeforeQualifier()
    {
        // arrange
        var source = Wrap("/* keep */ [|Derived|].Create();");
        var fixedSource = Wrap("/* keep */ Base.Create();");

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_OfferUseDeclaringTypeAction_When_QualifierIsDerivedType()
    {
        // arrange
        var source = Wrap("var x = [|Derived|].Create();");
        var fixedSource = Wrap("var x = Base.Create();");
        var test = CreateTest(source, fixedSource);
        test.CodeActionEquivalenceKey = "CCA0003";
        test.CodeActionVerifier =
            (action, verifier) => verifier.Equal("Use the declaring type", action.Title);

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Fix_Should_ReplaceCrefType_When_CrefParameterType()
    {
        // arrange
        var source =
            $$"""
            using Lib;

            class C
            {
                /// <see cref="Take([|Derived|].Nested)"/>
                public static void Take(Base.Nested n)
                {
                }
            }

            {{Lib}}
            """;
        var fixedSource =
            $$"""
            using Lib;

            class C
            {
                /// <see cref="Take(Base.Nested)"/>
                public static void Take(Base.Nested n)
                {
                }
            }

            {{Lib}}
            """;
        var test = CreateTest(source, fixedSource);
        test.SolutionTransforms.Add(
            (solution, projectId) => solution.WithProjectParseOptions(
                projectId,
                ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!)
                    .WithDocumentationMode(DocumentationMode.Diagnose)));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Fix_Should_ReplaceOuter_When_NestedQualifiers()
    {
        // arrange
        const string source =
            """
            class Base
            {
                public static void M()
                {
                }
            }

            class B
            {
                public class Inner : Base;
            }

            class D : B;

            class C
            {
                void Run()
                {
                    [|D.Inner|].M();
                }
            }
            """;
        const string fixedSource =
            """
            class Base
            {
                public static void M()
                {
                }
            }

            class B
            {
                public class Inner : Base;
            }

            class D : B;

            class C
            {
                void Run()
                {
                    Base.M();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_ReplaceTypeName_When_NestedTypeInDeclaration()
    {
        // arrange
        var source = Wrap("", members: "public [|Derived|].Nested item = null;");
        var fixedSource = Wrap("", members: "public Base.Nested item = null;");

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_WriteContainingType_When_DeclaringTypeIsNested()
    {
        // arrange
        const string source =
            """
            class Outer
            {
                public class Base
                {
                    public class Nested;
                }
            }

            class Derived : Outer.Base;

            class C
            {
                public [|Derived|].Nested item = null;
            }
            """;
        const string fixedSource =
            """
            class Outer
            {
                public class Base
                {
                    public class Nested;
                }
            }

            class Derived : Outer.Base;

            class C
            {
                public Outer.Base.Nested item = null;
            }
            """;

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_WriteGlobal_When_MemberHidesNamespace()
    {
        // arrange
        var source = Wrap(
            "var x = [|global::Lib.Derived|].Create();",
            members: "public int Lib => 0;",
            usings: string.Empty);
        var fixedSource = Wrap(
            "var x = global::Lib.Base.Create();",
            members: "public int Lib => 0;",
            usings: string.Empty);

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_WriteKeyword_When_DeclaringTypeIsObject()
    {
        // arrange
        var source = Wrap(
            "var x = [|Widget|].ReferenceEquals(a, b);",
            extra: "class Widget;");
        var fixedSource = Wrap(
            "var x = object.ReferenceEquals(a, b);",
            extra: "class Widget;");

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_WriteNamespace_When_NamespaceIsNotImported()
    {
        // arrange
        var source = Wrap("var x = [|Lib.Derived|].Create();", usings: string.Empty);
        var fixedSource = Wrap("var x = Lib.Base.Create();", usings: string.Empty);

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_WriteSimpleName_When_OriginalIsNamespaceQualified()
    {
        // arrange
        var source = Wrap(
            "var x = [|System.Text.UTF8Encoding|].Default;",
            usings: "using System.Text;");
        var fixedSource = Wrap("var x = Encoding.Default;", usings: "using System.Text;");

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task Fix_Should_WriteSimpleName_When_SimpleNameBinds()
    {
        // arrange
        var source = Wrap("var x = [|Derived|].Create();");
        var fixedSource = Wrap("var x = Base.Create();");

        // act & assert
        await VerifyAsync(source, fixedSource);
    }

    private static CSharpCodeFixTest<
        UseDeclaringTypeAnalyzer,
        UseDeclaringTypeCodeFixProvider,
        DefaultVerifier> CreateTest(string source, string fixedSource)
    {
        return new CSharpCodeFixTest<
            UseDeclaringTypeAnalyzer,
            UseDeclaringTypeCodeFixProvider,
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

    private static string Wrap(
        string body,
        string extra = "",
        string members = "",
        string usings = "using Lib;")
    {
        return
            $$"""
            using System;
            {{usings}}

            class C
            {
                {{members}}
                void M(object a, object b)
                {
                    {{body}}
                }
            }

            {{extra}}

            {{Lib}}
            """;
    }
}
