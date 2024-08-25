using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// The result of binding a script.
/// </summary>
/// <param name="GlobalBindings">The global variables defined in the script.</param>
/// <param name="FunctionBodies">The function bodies bound in the script.</param>
/// <param name="EvalType">The type the script evaluates to.</param>
internal readonly record struct ScriptBinding(
    ImmutableDictionary<VariableDeclarationSyntax, GlobalBinding> GlobalBindings,
    ImmutableDictionary<FunctionDeclarationSyntax, BoundStatement> FunctionBodies,
    TypeSymbol EvalType);
