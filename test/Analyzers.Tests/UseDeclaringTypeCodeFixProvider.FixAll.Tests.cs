namespace ChilliCream.Analyzers.Tests;

public sealed partial class UseDeclaringTypeCodeFixProviderTests
{
    [Fact]
    public async Task FixAll_Should_ReplaceBoth_When_NestedLambdas()
    {
        // arrange
        const string members =
            "static int Apply(int value, Func<int, int> f) => f(value);";
        var source = Wrap(
            "Apply(1, x => Apply(x, y => [|Derived|].Id(y)) + [|Derived|].Value);",
            members: members);
        var fixedSource = Wrap(
            "Apply(1, x => Apply(x, y => Base.Id(y)) + Base.Value);",
            members: members);
        var test = CreateTest(source, fixedSource);
        test.NumberOfFixAllIterations = 1;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FixAll_Should_ReplaceBoth_When_SiblingAccesses()
    {
        // arrange
        var source = Wrap("var x = [|Derived|].Value + [|Derived|].Count;");
        var fixedSource = Wrap("var x = Base.Value + Base.Count;");
        var test = CreateTest(source, fixedSource);
        test.NumberOfFixAllIterations = 1;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
