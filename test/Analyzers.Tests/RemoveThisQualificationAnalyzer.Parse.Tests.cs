namespace ChilliCream.Analyzers.Tests;

public sealed partial class RemoveThisQualificationAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_ReportThis_When_PreviousStatementEndsConditionalBlock()
    {
        // arrange
        var source = WithWidget(
            """
            public int Y;

            void M()
            {
            #if true
                _count = 1;
            #else
                _count = 2;
            #endif
                _count = [|this.|]Y;
            }
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_RemovalReparsesAsCast()
    {
        // arrange
        var source = WithWidget("int M(int value) => (this._map)(value);");

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportNothing_When_RemovalsTogetherReparseAsGenericCall()
    {
        // arrange
        var source = WithWidget(
            """
            private int A, B, C;

            static void F(bool x, bool y) { }

            void M(int d) => F(this.A < this.B, this.C > (d));
            """);

        // act & assert
        await VerifyAsync(source);
    }

    [Fact]
    public async Task Analyze_Should_ReportThis_When_ParenthesizedSwitchOperand()
    {
        // arrange
        var source = WithWidget("int M() => ([|this.|]_count) switch { 0 => 1, _ => 2 };");

        // act & assert
        await VerifyAsync(source);
    }
}
