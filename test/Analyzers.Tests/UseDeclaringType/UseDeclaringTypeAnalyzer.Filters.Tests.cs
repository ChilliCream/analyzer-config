namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeAnalyzerTests
{
    [Fact]
    public async Task
        Analyze_Should_ReportNothing_When_ContainingTypeOfDeclaringTypeIsObsolete()
    {
        // arrange
        const string source =
            """
            using System;

            [Obsolete("Old")]
            class Outer
            {
                public class Base
                {
                    public class Nested;
                }
            }

            #pragma warning disable CS0618
            class Derived : Outer.Base;
            #pragma warning restore CS0618

            class C
            {
                Derived.Nested item = null!;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_DeclaringTypeAssemblyIsExperimental()
    {
        // arrange
        const string source =
            """
            #pragma warning disable ASM1
            class Derived : Base;
            #pragma warning restore ASM1

            class C
            {
                Derived.Nested item = null!;

                void M()
                {
                    Derived.Make();
                }
            }
            """;

        const string library =
            """
            using System.Diagnostics.CodeAnalysis;

            [assembly: Experimental("ASM1")]

            public class Base
            {
                public class Nested;

                public static void Make()
                {
                }
            }
            """;

        var test = CreateTest(source);
        test.TestState.AdditionalProjects["Lib"].Sources.Add(("Lib.cs", library));
        test.TestState.AdditionalProjectReferences.Add("Lib");

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_DeclaringTypeIsEditorBrowsableAdvanced()
    {
        // arrange
        const string source =
            """
            using System.ComponentModel;

            [EditorBrowsable(EditorBrowsableState.Advanced)]
            class Base
            {
                public class Nested;
            }

            class Derived : Base;

            class C
            {
                Derived.Nested item = null!;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_DeclaringTypeIsEditorBrowsableNever()
    {
        // arrange
        const string source =
            """
            using System.ComponentModel;

            [EditorBrowsable(EditorBrowsableState.Never)]
            class Base
            {
                public class Nested;
            }

            class Derived : Base;

            class C
            {
                Derived.Nested item = null!;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_DeclaringTypeIsExperimental()
    {
        // arrange
        const string source =
            """
            using System.Diagnostics.CodeAnalysis;

            [Experimental("EXP1")]
            class Base
            {
                public class Nested;

                public static void Make()
                {
                }
            }

            #pragma warning disable EXP1
            class Derived : Base;
            #pragma warning restore EXP1

            class C
            {
                Derived.Nested item = null!;

                void M()
                {
                    Derived.Make();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_DeclaringTypeIsGeneric()
    {
        // arrange
        const string source =
            """
            class Base<T>
            {
                public class Nested;
            }

            class Derived : Base<int>;

            class C
            {
                Derived.Nested item = null!;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task
        Analyze_Should_ReportNothing_When_DeclaringTypeIsNestedInEnclosingGenericType()
    {
        // arrange
        const string source =
            """
            class Outer<T>
            {
                public class Base
                {
                    public class Nested;
                }

                public class Derived : Base;

                Derived.Nested item = null!;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_DeclaringTypeIsNestedInGenericType()
    {
        // arrange
        const string source =
            """
            class Outer<T>
            {
                public class Base
                {
                    public class Nested;
                }
            }

            class Derived : Outer<int>.Base;

            class C
            {
                Derived.Nested item = null!;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_DeclaringTypeIsObsolete()
    {
        // arrange
        const string source =
            """
            using System;

            [Obsolete("Old")]
            class Base
            {
                public class Nested;

                public static void Make()
                {
                }
            }

            #pragma warning disable CS0618
            class Derived : Base;
            #pragma warning restore CS0618

            class C
            {
                Derived.Nested item = null!;

                void M()
                {
                    Derived.Make();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_DeclaringTypeModuleIsExperimental()
    {
        // arrange
        const string source =
            """
            #pragma warning disable MOD1
            class Derived : Base;
            #pragma warning restore MOD1

            class C
            {
                {|MOD1:Derived.Nested|} item = null!;

                void M()
                {
                    {|MOD1:Derived.Make()|};
                }
            }
            """;

        const string library =
            """
            using System.Diagnostics.CodeAnalysis;

            [module: Experimental("MOD1")]

            public class Base
            {
                public class Nested;

                public static void Make()
                {
                }
            }
            """;

        var test = CreateTest(source);
        test.TestState.AdditionalProjects["Lib"].Sources.Add(("Lib.cs", library));
        test.TestState.AdditionalProjectReferences.Add("Lib");

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_DeclaringTypeIsEditorBrowsableAlways()
    {
        // arrange
        const string source =
            """
            using System.ComponentModel;

            [EditorBrowsable(EditorBrowsableState.Always)]
            class Base
            {
                public class Nested;
            }

            class Derived : Base;

            class C
            {
                [|Derived|].Nested item = null!;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }
}
