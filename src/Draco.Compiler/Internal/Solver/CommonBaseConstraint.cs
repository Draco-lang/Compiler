using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Solver;

internal class CommonBaseConstraint : Constraint
{
    public Type First { get; }
    public Type Second { get; }

    public ConstraintPromise<Type> Promise { get; }

    public CommonBaseConstraint(Type first, Type second)
    {
        this.First = first;
        this.Second = second;
        this.Promise = ConstraintPromise.Create<Type>(this);

    }
}
