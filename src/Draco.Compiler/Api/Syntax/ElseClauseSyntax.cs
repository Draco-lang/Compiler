namespace Draco.Compiler.Api.Syntax;

public partial class ElseClauseSyntax
{
    /// <summary>
    /// Returns <see langword="true"/> when the else clause is followed by an if expression.
    /// </summary>
    public bool IsElseIf => this.Expression is StatementExpressionSyntax statementExpression
                            && statementExpression.Statement is ExpressionStatementSyntax expressionStatement
                            && expressionStatement.Expression is IfExpressionSyntax;
}
