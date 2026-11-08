namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeAnalyzerTests
{
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
