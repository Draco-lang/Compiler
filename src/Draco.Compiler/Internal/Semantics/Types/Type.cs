using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.Types;

/// <summary>
/// The base for all types in the compiler.
/// </summary>
internal abstract partial record class Type
{
    /// <summary>
    /// Unwraps the type if it's a type-variable.
    /// </summary>
    public virtual Type UnwrapTypeVariable => this;

    /// <summary>
    /// True, if this is an error type.
    /// </summary>
    public virtual bool IsError => false;

    /// <summary>
    /// All diagnostics related to this type.
    /// </summary>
    public virtual ImmutableArray<Diagnostic> Diagnostics => ImmutableArray<Diagnostic>.Empty;
}

// Builtins
internal abstract partial record class Type
{
    public static readonly Type Never_ = Never.Instance;
    public static readonly Type Unit = new Builtin(typeof(void));
    public static readonly Type Int32 = new Builtin(typeof(int));
    public static readonly Type Float64 = new Builtin(typeof(double));
    public static readonly Type Bool = new Builtin(typeof(bool));
    public static readonly Type String = new Builtin(typeof(string));
    // TODO: Our characters should be 32 bits, this does not suffice
    public static readonly Type Char = new Builtin(typeof(char));
}

internal abstract partial record class Type
{
    /// <summary>
    /// Type variables are uninferred types that can be substituted to regular types or other variables.
    /// </summary>
    public sealed record class Variable : Type
    {
        private static int idCounter = -1;

        public override Type UnwrapTypeVariable =>
            this.substitution ?? this;

        private readonly int id = Interlocked.Increment(ref idCounter);
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
                if (this.substitution is Variable var) this.substitution = var.Substitution;
                return this.substitution;
            }
            set
            {
                if (this.substitution is not null) throw new InvalidOperationException("tried to substitute type variable multiple times");
                this.substitution = value;
            }
        }

        public ParseNode? Defitition { get; }

        public Variable(ParseNode? defitition)
        {
            this.Defitition = defitition;
        }

        public override string ToString() => this.substitution is null
            ? $"{StringUtils.IndexToExcelColumnName(this.id)}'"
            : this.substitution.ToString();

        public bool Equals(Variable? other) => throw new InvalidOperationException("can't compare type variables");
        public override int GetHashCode() => throw new InvalidOperationException("can't hash type variables");
    }
}

internal abstract partial record class Type
{
    /// <summary>
    /// Represents an error type in a type error.
    /// </summary>
    /// <param name="Diagnostics">The <see cref="Diagnostic"/> messages related to the type error.</param>
    public sealed record class Error(ImmutableArray<Diagnostic> Diagnostics) : Type
    {
        public static Error Empty { get; } = new(ImmutableArray<Diagnostic>.Empty);

        public override bool IsError => true;
        public override ImmutableArray<Diagnostic> Diagnostics { get; } = Diagnostics;

        public override string ToString() => "<error>";

        public bool Equals(Error? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}

internal abstract partial record class Type
{
    /// <summary>
    /// Represents the bottom-type.
    /// </summary>
    public sealed record class Never : Type
    {
        public static Never Instance { get; } = new();

        private Never()
        {
        }

        public override string ToString() => "<never>";
    }
}

internal abstract partial record class Type
{
    /// <summary>
    /// Represents a native, builtin type.
    /// </summary>
    public sealed record class Builtin(System.Type Type) : Type
    {
        public override string ToString() => this.Type.Name;

        public bool Equals(Builtin? other) => this.Type.Equals(other?.Type);
        public override int GetHashCode() => this.Type.GetHashCode();
    }
}

internal abstract partial record class Type
{
    /// <summary>
    /// Represents a function type.
    /// </summary>
    public sealed record class Function(ImmutableArray<Type> Params, Type Return) : Type
    {
        public override string ToString() => $"({string.Join(", ", this.Params)}) -> {this.Return}";

        public bool Equals(Function? other) =>
               other is not null
            && this.Params.Length == other.Params.Length
            && this.Return.Equals(other.Return);
        public override int GetHashCode()
        {
            var h = default(HashCode);
            foreach (var p in this.Params) h.Add(p);
            h.Add(this.Return);
            return h.ToHashCode();
        }
    }
}
