using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// A visitor that does type-inference on the given subtree.
/// </summary>
internal sealed class TypeInferenceVisitor : ParseTreeVisitorBase<Unit>
{
    /// <summary>
    /// Type variables are uninferred types that can be substituted to regular types or other variables.
    /// We define type-variables here, but make sure they never escape this type.
    /// </summary>
    private sealed record class TypeVar : Type
    {
        private Type? substitution;

        /// <summary>
        /// The substitution of this type variable.
        /// </summary>
        public Type Substitution
        {
            get
            {
                if (this.substitution is null) return this;
                // Pruning
                if (this.substitution is TypeVar var) this.substitution = var.Substitution;
                return this.substitution;
            }
            set
            {
                if (this.substitution is not null) throw new InvalidOperationException("tried to substitute type variable multiple times");
                this.substitution = value;
            }
        }
    }

    public ImmutableDictionary<Symbol, Type> Result => this.types
        .ToImmutableDictionary(kv => kv.Key, kv => this.RemoveSubstitutions(kv.Value));

    private readonly Dictionary<Symbol, Type> types = new();

    /// <summary>
    /// Removes type variable substitutions.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to remove substitutions from.</param>
    /// <returns>The equivalent of <paramref name="type"/> without any variable substitutions.</returns>
    private Type RemoveSubstitutions(Type type) => throw new NotImplementedException();
}
