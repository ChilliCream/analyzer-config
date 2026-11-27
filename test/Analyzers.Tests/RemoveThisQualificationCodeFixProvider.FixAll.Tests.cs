namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveThisQualificationCodeFixProviderTests
{
    [Fact]
    public async Task FixAll_Should_RemoveAll_When_NestedLambdas()
    {
        // arrange
        var source = WithWidget(
            "Func<int, Func<int>> M() => a => () => [|this.|]Scale(a) + [|this.|]_count;");
        var fixedSource = WithWidget(
            "Func<int, Func<int>> M() => a => () => Scale(a) + _count;");
        var test = CreateTest(source, fixedSource);
        test.NumberOfFixAllIterations = 1;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FixAll_Should_RemoveAll_When_SiblingAccesses()
    {
        // arrange
        var source = WithWidget(
            "int M() => [|this.|]_count + [|this.|]Count + [|this.|]Scale(1);");
        var fixedSource = WithWidget("int M() => _count + Count + Scale(1);");
        var test = CreateTest(source, fixedSource);
        test.NumberOfFixAllIterations = 1;

        // act & assert
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
