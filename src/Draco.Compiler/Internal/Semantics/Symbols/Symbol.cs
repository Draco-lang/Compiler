using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Semantics.Types;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ApiSymbol = Draco.Compiler.Api.Semantics.Symbol;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics.Symbols;

/// <summary>
/// The base of all symbols.
/// </summary>
internal abstract partial class Symbol
{
    /// <summary>
    /// The name of this symbol that it can be referenced by.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// True, if this symbol represents an error.
    /// </summary>
    public virtual bool IsError => false;

    /// <summary>
    /// The diagnostics related to this symbol.
    /// </summary>
    public virtual ImmutableArray<Diagnostic> Diagnostics => ImmutableArray<Diagnostic>.Empty;

    /// <summary>
    /// The enclosing <see cref="Scope"/> of this symbol.
    /// </summary>
    public abstract Scope? EnclosingScope { get; }

    /// <summary>
    /// The syntax node that defined this symbol.
    /// </summary>
    public abstract ParseTree? Definition { get; }

    protected Symbol(string name)
    {
        this.Name = name;
    }

    /// <summary>
    /// Converts this <see cref="Symbol"/> to an <see cref="ApiSymbol"/>.
    /// </summary>
    /// <returns>The equivalent <see cref="ApiSymbol"/>.</returns>
    public ApiSymbol ToApiSymbol() => new(this);
}

internal abstract partial class Symbol
{
    // Base class for "real" symbols found in the tree
    public abstract class InTreeBase : Symbol
    {
        public override ParseTree Definition { get; }
        public override Scope? EnclosingScope =>
            SymbolResolution.GetContainingScopeOrNull(this.db, this.Definition);

        private readonly QueryDatabase db;

        protected InTreeBase(QueryDatabase db, string name, ParseTree definition)
            : base(name)
        {
            this.db = db;
            this.Definition = definition;
        }
    }
}

internal abstract partial class Symbol
{
    /// <summary>
    /// A symbol for a reference error.
    /// </summary>
    public sealed class Error : Symbol
    {
        public override Scope? EnclosingScope => null;
        public override bool IsError => true;
        public override ParseTree? Definition => null;
        public override ImmutableArray<Diagnostic> Diagnostics { get; }

        public Error(string name, ImmutableArray<Diagnostic> diagnostics)
            : base(name)
        {
            this.Diagnostics = diagnostics;
        }
    }
}

internal abstract partial class Symbol
{
    /// <summary>
    /// A symbol for a label.
    /// </summary>
    public sealed class Label : InTreeBase
    {
        public Label(QueryDatabase db, string name, ParseTree definition)
            : base(db, name, definition)
        {
        }
    }
}

internal abstract partial class Symbol
{
    /// <summary>
    /// A symbol for a function declaration.
    /// </summary>
    public sealed class Function : InTreeBase
    {
        public Function(QueryDatabase db, string name, ParseTree definition)
            : base(db, name, definition)
        {
        }
    }
}

internal abstract partial class Symbol
{
    /// <summary>
    /// A symbol for a variable declaration.
    /// </summary>
    public sealed class Variable : InTreeBase
    {
        public bool IsMutable { get; }

        public Variable(QueryDatabase db, string name, ParseTree definition, bool isMutable)
            : base(db, name, definition)
        {
            this.IsMutable = isMutable;
        }
    }
}

internal abstract partial class Symbol
{
    /// <summary>
    /// A symbol types.
    /// </summary>
    public sealed class TypeAlias : Symbol
    {
        public Type Type { get; }
        // TODO
        public override Scope? EnclosingScope => throw new NotImplementedException();
        public override ParseTree? Definition => throw new NotImplementedException();

        public TypeAlias(string name, Type type)
            : base(name)
        {
            this.Type = type;
        }
    }
}

// NOTE: Temporary
internal abstract partial class Symbol
{
    /// <summary>
    /// A symbol for intrinsics.
    /// </summary>
    public sealed class Intrinsic : Symbol
    {
        public override Scope? EnclosingScope => null;
        public override ParseTree? Definition => null;

        public Intrinsic(string name)
            : base(name)
        {
        }
    }
}
