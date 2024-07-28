using System.Collections.Immutable;
using System.Linq;
using Draco.Chr.Constraints;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private FunctionTypeSymbol MakeMismatchedFunctionType(ImmutableArray<Argument> args, TypeSymbol returnType) => new(
        args
            // TODO: We are passing null here...
            .Select(a => new SynthetizedParameterSymbol(null!, a.Type))
            .Cast<ParameterSymbol>()
            .ToImmutableArray(),
        returnType);

    private FunctionSymbol GenericInstantiateIfNeeded(FunctionSymbol chosen)
    {
        // Nongeneric, just return
        if (!chosen.IsGenericDefinition) return chosen;

        // Implicit generic instantiation
        // Create the proper number of type variables as type arguments
        var typeArgs = Enumerable
            .Range(0, chosen.GenericParameters.Length)
            .Select(_ => this.AllocateTypeVariable())
            .Cast<TypeSymbol>()
            .ToImmutableArray();

        // Instantiate the chosen symbol
        var instantiated = chosen.GenericInstantiate(chosen.ContainingSymbol, typeArgs);
        return instantiated;
    }

    private static void AssignParameterToArgument(
        ConstraintStore store, TypeSymbol paramType, Argument argument) => store.Add(new Constraints.Assignable(
            locator: ConstraintLocator.Syntax(argument.Syntax),
            targetType: paramType,
            assignedType: argument.Type));
}
