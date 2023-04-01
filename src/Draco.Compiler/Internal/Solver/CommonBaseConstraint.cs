using System.Collections.Generic;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Solver;

internal class CommonBaseConstraint : Constraint
{
    public IEnumerable<Type> Types { get; }

    public ConstraintPromise<Type> Promise { get; }

    public CommonBaseConstraint(params Type[] types)
    {
        this.Types = types;
        this.Promise = ConstraintPromise.Create<Type>(this);

    }
}
