namespace Draco.Compiler.Api.Syntax;

public partial class ElseClauseSyntax
{
    public bool IsElseIf => this.Expression is StatementExpressionSyntax statementExpression
                            && statementExpression.Statement is ExpressionStatementSyntax expressionStatement
                            && expressionStatement.Expression is IfExpressionSyntax;
}
