using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeAnalyzerTests
{
    private const string ExpressionLib =
        """
        namespace Lib
        {
            public class Base
            {
                public static int Value;
                public const int Limit = 1;
                public static int Count => 0;
                public static event System.Action Changed;
                public static Base Create() => new();
                public static T Id<T>(T value) => value;
                public static int Parse(string text) => 0;

                public class Nested
                {
                    public static Nested Make() => new();
                }
            }

            public class Derived : Base;
        }
        """;

    [Fact]
    public async Task Analyze_Should_ReplaceWithGlobal_When_MemberHidesNamespace()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:global::Lib.Derived|}.Create();",
            members: "public int Lib => 0;",
            useLib: false);

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Create", "global::Lib.Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReplaceWithNamespace_When_LocalHidesSimpleName()
    {
        // arrange
        var source = WrapExpression(
            "var Base = 1;\nvar x = {|#0:Derived|}.Create();");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Create", "Lib.Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReplaceWithSimpleName_When_ColorColorProperty()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:Derived|}.Create();",
            members: "public Base Base { get; } = new();");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Create", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_AccessHasError()
    {
        // arrange
        var source = WrapExpression(
            "Derived.{|CS1501:Create|}(1);");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_GenericQualifierDeclaresMember()
    {
        // arrange
        const string source =
            """
            class Box<T>
            {
                public static int Size;
            }

            class C
            {
                int M() => Box<int>.Size;
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_NoFormNamesDeclaringType()
    {
        // arrange
        const string source =
            """
            extern alias Old;

            class C
            {
                void M() => Old::Lib.Derived.Create();
            }
            """;

        const string library =
            """
            namespace Lib
            {
                public class Base
                {
                    public static Base Create() => new();
                }

                public class Derived : Base;
            }
            """;

        var test = CreateTest(source);
        test.TestState.AdditionalProjects["OldLib"].Sources.Add(("Lib.cs", library));
        test.TestState.AdditionalProjectReferences.Add("OldLib");
        test.SolutionTransforms.Add(
            (solution, projectId) =>
            {
                var project = solution.GetProject(projectId)!;
                var references = project.ProjectReferences
                    .Select(r => new ProjectReference(r.ProjectId, ["Old"]))
                    .ToList();

                return solution.WithProjectReferences(projectId, references);
            });

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_PropertyHidesMethodByName()
    {
        // arrange
        const string source =
            """
            using System;

            class B
            {
                public static int M(int x) => x;
            }

            class D : B
            {
                public static new Func<int, int> M => x => -x;
            }

            static class P
            {
                static int Run() => D.M(1);
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_QualifierContainsComment()
    {
        // arrange
        var source = WrapExpression(
            "var x = Lib./* keep */Derived.Create();");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_QualifierDeclaresMember()
    {
        // arrange
        var source = WrapExpression(
            "var x = Base.Create();");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_QualifierHasError()
    {
        // arrange
        const string source =
            """
            using System;

            class Base
            {
                public static Base Create() => new();
            }

            [Obsolete("x", true)]
            class Derived : Base;

            class C
            {
                void M()
                {
                    var x = {|CS0619:Derived|}.Create();
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_QualifierHidesMember()
    {
        // arrange
        var source = WrapExpression(
            "var x = Hider.Create();",
            extra: "class Hider : Base { public static new Base Create() => new(); }");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_RewriteMakesEnclosingCallAmbiguous()
    {
        // arrange
        const string source =
            """
            using System;

            class B
            {
                public static int M(int x) => x;
                public static int M(object o) => 0;
            }

            class D : B
            {
                public static string M(string s) => s;
            }

            static class P
            {
                static void Use(Func<int, int> f) { }
                static void Use(Func<string, int> f) { }

                static void Run()
                {
                    Use(x => D.M(x));
                    Use(x => { return D.M(x); });
                }
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_StaticExtensionMember()
    {
        // arrange
        var source = WrapExpression(
            "Derived.Touch();",
            extra:
                "static class E { extension(Derived) { public static void Touch() { } } }");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_TypeParameterQualifier()
    {
        // arrange
        const string source =
            """
            interface IMake<T> where T : IMake<T>
            {
                static abstract T Make();
            }

            class C
            {
                static T Run<T>() where T : IMake<T> => T.Make();
            }
            """;

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_AliasQualifier()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:Alias|}.Create();",
            usings: "using Alias = Lib.Derived;");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Create", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_AssignmentTarget()
    {
        // arrange
        var source = WrapExpression(
            "[|Derived|].Value = 1;");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_BaseInterfaceMember()
    {
        // arrange
        const string source =
            """
            interface I1
            {
                static void Reset() { }
            }

            interface I2 : I1;

            class C
            {
                void M() => {|#0:I2|}.Reset();
            }
            """;

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Reset", "I1"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_Constant()
    {
        // arrange
        var source = WrapExpression(
            "var x = [|Derived|].Limit;");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_EnclosingCallKeepsOverload()
    {
        // arrange
        var source = WrapExpression(
            "Apply(1, x => [|Derived|].Id(x));\nApply(1, x => { return [|Derived|].Id(x); });",
            members: "static T Apply<T>(T seed, Func<T, T> map) => map(seed);");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_EnumMember()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:DayOfWeek|}.GetNames(typeof(DayOfWeek));");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("GetNames", "Enum"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_GenericMethod()
    {
        // arrange
        var source = WrapExpression(
            "var x = [|Derived|].Id(1);");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_GenericQualifier()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:Task<int>|}.Delay(1);");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Delay", "Task"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_GlobalQualified()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:global::Lib.Derived|}.Create();");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Create", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_InsideQualifierType()
    {
        // arrange
        var source = WrapExpression(
            "",
            extra: "class Sub : Base { void Run() => {|#0:Sub|}.Create(); }");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Create", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_InterpolationOrSwitchArm()
    {
        // arrange
        var source = WrapExpression(
            "var s = $\"{[|Derived|].Value}\";\nvar t = n switch { _ => [|Derived|].Value };");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_InvokedMethod()
    {
        // arrange
        var source = WrapExpression(
            "var x = [|Derived|].Create();");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_KeywordQualifier()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:string|}.ReferenceEquals(a, b);");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("ReferenceEquals", "object"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_MethodGroup()
    {
        // arrange
        var source = WrapExpression(
            "var x = texts.Select([|Derived|].Parse);");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_NamespaceQualified()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:Lib.Derived|}.Create();");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Create", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_NestedTypeInExpression()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:Derived|}.Nested.Make();");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Nested", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_ObjectMember()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:Widget|}.ReferenceEquals(a, b);",
            extra: "class Widget;");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("ReferenceEquals", "object"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_StaticEvent()
    {
        // arrange
        var source = WrapExpression(
            "[|Derived|].Changed += handler;");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_StaticField()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:Derived|}.Value;");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Value", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_StaticMemberOfBaseOfBase()
    {
        // arrange
        var source = WrapExpression(
            "var x = {|#0:Further|}.Create();",
            extra: "class Further : Derived;");

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CCA0003", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("Create", "Base"));

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Analyze_Should_ReportQualifier_When_StaticProperty()
    {
        // arrange
        var source = WrapExpression(
            "var x = [|Derived|].Count;");

        // act & assert
        await VerifyAsync(source);
    }

    private static string WrapExpression(
        string body,
        string extra = "",
        string members = "",
        string usings = "",
        bool useLib = true)
    {
        var lib = useLib ? "using Lib;\n" : string.Empty;

        return $$"""
            using System;
            using System.Linq;
            using System.Threading.Tasks;
            {{lib}}{{usings}}

            class C
            {
                {{members}}
                void M(Action handler, string[] texts, object a, object b, int n)
                {
                    {{body}}
                }
            }

            {{extra}}

            {{ExpressionLib}}
            """;
    }
}
