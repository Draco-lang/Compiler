using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// The base for all types in the compiler.
/// </summary>
internal abstract partial record class Type
{
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
    public static readonly Type Unit = new Builtin(typeof(void));
    public static readonly Type Int32 = new Builtin(typeof(int));
}

internal abstract partial record class Type
{
    /// <summary>
    /// Represents an error type in a type error.
    /// </summary>
    /// <param name="Diagnostics">The <see cref="Diagnostic"/> messages related to the type error.</param>
    public sealed record class Error(ImmutableArray<Diagnostic> Diagnostics) : Type
    {
        public override bool IsError => true;
        public override ImmutableArray<Diagnostic> Diagnostics { get; } = Diagnostics;

        public override string ToString() => "<error>";

        public bool Equals(Builtin? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
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
