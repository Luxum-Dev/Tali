public sealed class LiteralExpressionSyntax : ExpressionSyntax
{

    public LiteralExpressionSyntax(SyntaxToken literalToken)
    {
        LiteralToken = literalToken;
    }

    public SyntaxToken LiteralToken { get; }
    public override SyntaxKind Kind => SyntaxKind.NumberExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return LiteralToken;
    }
}
