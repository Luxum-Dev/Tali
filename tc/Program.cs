﻿while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line))
        return;

    var parser = new Parser(line);
    var syntaxTree = parser.Parse();

    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Gray;
    PrettyPrint(syntaxTree.Root);
    Console.ForegroundColor = color;

    if (syntaxTree.Diagnostics.Any())
    {
        Console.ForegroundColor = ConsoleColor.Red;

        foreach (var diagnostic in syntaxTree.Diagnostics)
        {
            Console.WriteLine(diagnostic);
        }

        Console.ForegroundColor = color;
    }
}

static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true)
{
    var marker = isLast ? "└──" : "├──";

    Console.Write(indent);
    Console.Write(marker);
    Console.Write(node.Kind);

    if (node is SyntaxToken t && t.Value != null)
    {
        Console.Write(" ");
        Console.Write(t.Value);
    }

    Console.WriteLine();

    indent += isLast ? "\t" : "│\t";

    var lastChild = node.GetChildren().LastOrDefault();

    foreach (var child in node.GetChildren())
        PrettyPrint(child, indent, child == lastChild);
}

enum SyntaxKind
{
    NumberToken,
    WhiteSpaceToken,
    PlusToken,
    MinusToken,
    StarToken,
    SlashToken,
    OpenParenthesisToken,
    CloseParenthesisToken,
    BadToken,
    EndOfFileToken,
    NumberExpression,
    BinaryExpression
}

class SyntaxToken : SyntaxNode
{
    public SyntaxToken(SyntaxKind kind, int position, string? text, object? value)
    {
        Kind = kind;
        Position = position;
        Text = text;
        Value = value;
    }

    public override SyntaxKind Kind { get; }
    public int Position { get; }
    public string? Text { get; }
    public object? Value { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        return Enumerable.Empty<SyntaxNode>();
    }
}

class Lexer
{
    private readonly string _text;
    private int _position;
    private List<string> _diagnostics = new List<string>();
    private char Current
    {
        get
        {
            if (_position >= _text.Length)
                return '\0';

            return _text[_position];
        }
    }

    private void Next()
    {
        _position++;
    }
    public Lexer(string text)
    {
        _text = text;
    }

    public IEnumerable<string> Diagnostic => _diagnostics;

    public SyntaxToken NextToken()
    {

        if (_position >= _text.Length)
        {
            return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "\0", null);
        }

        if (char.IsDigit(Current))
        {
            var start = _position;
            while (char.IsDigit(Current))
                Next();

            var length = _position - start;
            var text = _text.Substring(start, length);
            int.TryParse(text, out var value);
            return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
        }

        if (char.IsWhiteSpace(Current))
        {
            var start = _position;
            while (char.IsWhiteSpace(Current))
                Next();

            var length = _position - start;
            var text = _text.Substring(start, length);
            return new SyntaxToken(SyntaxKind.WhiteSpaceToken, start, text, null);
        }

        if (Current == '+')
        {
            return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);
        }
        else if (Current == '-')
        {
            return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);
        }
        else if (Current == '*')
        {
            return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);
        }
        else if (Current == '/')
        {
            return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);
        }
        else if (Current == '(')
        {
            return new SyntaxToken(SyntaxKind.OpenParenthesisToken, _position++, "(", null);
        }
        else if (Current == ')')
        {
            return new SyntaxToken(SyntaxKind.CloseParenthesisToken, _position++, ")", null);
        }
        _diagnostics.Add($"ERROR: bad character input: '{Current}'");
        return new SyntaxToken(SyntaxKind.BadToken, _position++, _text.Substring(_position - 1, 1), null);
    }

}

abstract class SyntaxNode
{
    public abstract SyntaxKind Kind { get; }

    public abstract IEnumerable<SyntaxNode> GetChildren();
}

class ExpressionSyntax : SyntaxNode
{
    public override SyntaxKind Kind => throw new NotImplementedException();

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        throw new NotImplementedException();
    }
}

sealed class NumberExpressionSyntax : ExpressionSyntax
{

    public NumberExpressionSyntax(SyntaxToken numberToken)
    {
        NumberToken = numberToken;
    }

    public SyntaxToken NumberToken { get; }
    public override SyntaxKind Kind => SyntaxKind.NumberExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return NumberToken;
    }
}

sealed class BinaryExpressionSyntax : ExpressionSyntax
{
    public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
    {
        Left = left;
        OperatorToken = operatorToken;
        Right = right;
    }

    public ExpressionSyntax Left { get; }
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Right { get; }

    public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return OperatorToken;
        yield return Right;
    }
}

sealed class SyntaxTree
{
    public SyntaxTree(IEnumerable<string> diagnostics, ExpressionSyntax root, SyntaxToken endOfFileToken)
    {
        Diagnostics = diagnostics.ToArray();
        Root = root;
        EndOfFileToken = endOfFileToken;
    }

    public IReadOnlyList<string> Diagnostics { get; }
    public ExpressionSyntax Root { get; }
    public SyntaxToken EndOfFileToken { get; }
}

class Parser
{
    private readonly SyntaxToken[] _tokens;
    private int _position;
    private List<string> _diagnostics = new List<string>();

    public Parser(string text)
    {
        var lexer = new Lexer(text);
        var tokens = new List<SyntaxToken>();
        SyntaxToken token;

        do
        {
            token = lexer.NextToken();

            if (token.Kind != SyntaxKind.WhiteSpaceToken && token.Kind != SyntaxKind.BadToken)
            {
                tokens.Add(token);
            }

        } while (token.Kind != SyntaxKind.EndOfFileToken);

        _tokens = tokens.ToArray();
        _diagnostics.AddRange(lexer.Diagnostic);
    }

    public IEnumerable<string> Diagnostics => _diagnostics;

    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _tokens.Length)
        {
            return _tokens[_tokens.Length - 1];
        }
        return _tokens[index];
    }

    private SyntaxToken Current => Peek(0);

    private SyntaxToken NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }

    private SyntaxToken Match(SyntaxKind kind)
    {
        if (Current.Kind == kind)
        {
            return NextToken();
        }
        _diagnostics.Add($"Error : Unexpected token <{Current.Kind}>, expected <{kind}>");
        return new SyntaxToken(kind, Current.Position, null, null);
    }

    public SyntaxTree Parse()
    {
        var expression = ParseExpression();
        var endOfFileToken = Match(SyntaxKind.EndOfFileToken);
        return new SyntaxTree(_diagnostics, expression, endOfFileToken);
    }

    private ExpressionSyntax ParseExpression()
    {
        var left = ParsePrimaryExpression();

        while (Current.Kind == SyntaxKind.PlusToken || Current.Kind == SyntaxKind.MinusToken)
        {
            var operatorToken = NextToken();
            var right = ParsePrimaryExpression();
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        var numberToken = Match(SyntaxKind.NumberToken);
        return new NumberExpressionSyntax(numberToken);
    }
}
