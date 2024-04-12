using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Chr.Rules;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private static IEnumerable<Rule> ConstructRules() => [];
}
