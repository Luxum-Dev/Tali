public class ExpressionSyntax : SyntaxNode
{
    public override SyntaxKind Kind => throw new NotImplementedException();

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        throw new NotImplementedException();
    }
}
