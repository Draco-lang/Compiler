using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.Types;

/// <summary>
/// A visitor that does type-inference on the given subtree.
/// </summary>
internal sealed class TypeInferenceVisitor : ParseTreeVisitorBase<Unit>
{
    public ImmutableDictionary<Symbol, Type> Result => this.types
        .ToImmutableDictionary(kv => kv.Key, kv => this.RemoveSubstitutions(kv.Value));

    public IReadOnlyDictionary<Symbol, Type> Types => this.types;

    private readonly ConstraintSolver solver = new();
    private readonly QueryDatabase db;
    private readonly Dictionary<Symbol, Type> types = new();

    public TypeInferenceVisitor(QueryDatabase db)
    {
        this.db = db;
    }

    /// <summary>
    /// Removes type variable substitutions.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to remove substitutions from.</param>
    /// <returns>The equivalent of <paramref name="type"/> without any variable substitutions.</returns>
    private Type RemoveSubstitutions(Type type) => type switch
    {
        Type.Builtin => type,
        Type.Variable v => this.UnwrapTypeVariable(v),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private Type UnwrapTypeVariable(Type.Variable v)
    {
        var result = v.Substitution;
        if (result is Type.Variable)
        {
            // TODO: Not necessarily the defined symbol...
            var symbol = v.Defitition is null
                ? null
                : SymbolResolution.GetDefinedSymbolOrNull(this.db, v.Defitition);
            var diag = Diagnostic.Create(
                template: SemanticErrors.CouldNotInferType,
                location: v.Defitition is null ? Location.None : new Location.ToTree(v.Defitition),
                formatArgs: symbol?.Name);
            return new Type.Error(ImmutableArray.Create(diag));
        }
        return result;
    }

    public override Unit VisitVariableDecl(ParseTree.Decl.Variable node)
    {
        // The symbol we are inferring the type for
        var symbol = SymbolResolution.GetDefinedSymbolOrNull(this.db, node);
        Debug.Assert(symbol is not null);

        // The declared type after the ':' and the value type after the '='
        var declaredType = node.Type is not null
            ? TypeChecker.Evaluate(this.db, node.Type.Type)
            : null;
        var valueType = node.Initializer is not null
            ? TypeChecker.TypeOf(this.db, node.Initializer.Value)
            : null;

        // Infer the type from the two potential sources
        var inferredType = null as Type;
        if (declaredType is null && valueType is null)
        {
            // var x;
            // Just a new type variable, will need to infer from context
            inferredType = new Type.Variable(node);
        }
        else if (declaredType is null || valueType is null)
        {
            // var x: T;
            // var x = v;
            // Whatever is non-null
            inferredType = declaredType ?? valueType;
        }
        else
        {
            // var x: T = v;
            // TODO: Need to put a constraint that valueType is subtype of declaredType
            inferredType = declaredType;
            throw new NotImplementedException();
        }

        // Store the inferred type
        Debug.Assert(inferredType is not null);
        this.types[symbol] = inferredType;

        // Inference in children
        return base.VisitVariableDecl(node);
    }

    public override Unit VisitBinaryExpr(ParseTree.Expr.Binary node)
    {
        if (node.Operator.Type == TokenType.Assign)
        {
            // Right has to be assignable to left
            var leftType = TypeChecker.TypeOf(this.db, node.Left);
            var rightType = TypeChecker.TypeOf(this.db, node.Right);
            this.solver.Assignable(leftType, rightType);
        }
        else
        {
            // TODO
        }
        // Inference in children
        return base.VisitBinaryExpr(node);
    }
}
