using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint representing that a type is expected to have a certain named member.
/// </summary>
internal sealed class MemberConstraint : Constraint
{
    /// <summary>
    /// The type of the accessed object.
    /// </summary>
    public TypeSymbol Accessed { get; }

    /// <summary>
    /// The name of the accessed member.
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// The type of the member.
    /// </summary>
    public TypeSymbol MemberType { get; }

    /// <summary>
    /// The promise of the accessed member symbol.
    /// </summary>
    public ConstraintPromise<Symbol> Promise { get; }

    public MemberConstraint(TypeSymbol accessed, string memberName, TypeSymbol memberType)
    {
        this.Accessed = accessed;
        this.MemberName = memberName;
        this.MemberType = memberType;
        this.Promise = ConstraintPromise.Create<Symbol>(this);
    }
}
