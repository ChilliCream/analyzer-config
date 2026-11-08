using Microsoft.CodeAnalysis;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveRedundantTypeArgumentsAnalyzerTests
{
    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_LocalFunctionTypeParameterInExpressionBody(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class C
            {
                void M()
                {
                    static T Wrap<T>(T value) where T : struct => H.Id<T>(value);
                }
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportNothing_When_RecursiveLocalFunctionCall(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class C
            {
                void M()
                {
                    static T Pick<T>(T value, bool again) where T : struct
                        => again ? Pick<T>(value, false) : value;
                }
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_AnonymousMethod(NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                void M()
                {
                    Action act = delegate { H.Id[|<string>|]("x"); };
                }
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_BlockLambdaInFieldInitializer(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static readonly Func<string> s_factory = () => { return H.Id[|<string>|]("x"); };
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_BlockLambdaInMethod(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Run<T>(Func<T> factory) => factory();

                void M()
                {
                    Run(() => { return H.Id[|<string>|]("x"); });
                }
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_ConstructorInitializer(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class B
            {
                protected B(string value)
                {
                }
            }

            class D : B
            {
                D() : base(H.Id[|<string>|]("x"))
                {
                }
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_ExpressionBodiedAccessor(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class C
            {
                string Name { get => H.Id[|<string>|]("x"); }
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_ExpressionBodiedMethod(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class C
            {
                string M() => H.Id[|<string>|]("x");
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_ExpressionBodiedProperty(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class C
            {
                string Name => H.Id[|<string>|]("x");
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_FieldInitializer(NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class C
            {
                static readonly string s_field = H.Id[|<string>|]("x");
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_LambdaInsideLambda(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System;

            class C
            {
                static T Run<T>(Func<T> factory) => factory();

                void M()
                {
                    Run(() => Run(() => H.Id[|<string>|]("x")));
                }
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_LocalFunctionExpressionBody(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class C
            {
                void M()
                {
                    string F() => H.Id[|<string>|]("x");
                }
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_PrimaryConstructorBase(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class B(string value);

            class E(string value) : B(H.Id[|<string>|](value));

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_PropertyInitializer(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class C
            {
                string Name { get; } = H.Id[|<string>|]("x");
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_QueryExpressionSource(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            using System.Linq;

            class C
            {
                void M(int[] numbers)
                {
                    var query = from x in H.Id[|<int[]>|](numbers) select x;
                }
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_SwitchExpressionArm(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            class C
            {
                void M(bool flag)
                {
                    var name = flag switch { true => H.Id[|<string>|]("x"), _ => "y" };
                }
            }

            static class H
            {
                public static T Id<T>(T value) => value;
            }
            """;

        // act & assert
        await VerifyAsync(source, nullable);
    }

    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task
        Analyze_Should_ReportTypeArgumentList_When_TopLevelStatement(
            NullableContextOptions nullable)
    {
        // arrange
        const string source =
            """
            static T Id<T>(T value) => value;

            var name = Id[|<string>|]("x");
            """;

        var test = CreateTest(source, nullable);
        test.TestState.OutputKind = OutputKind.ConsoleApplication;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
