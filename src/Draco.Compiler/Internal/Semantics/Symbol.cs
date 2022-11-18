using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Query;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ApiSymbol = Draco.Compiler.Api.Semantics.Symbol;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// The base of all symbols.
/// </summary>
internal abstract partial class Symbol
{
    public string Name { get; }
    public virtual bool IsError => false;
    public virtual ImmutableArray<Diagnostic> Diagnostics => ImmutableArray<Diagnostic>.Empty;
    public abstract Scope? EnclosingScope { get; }
    public abstract ParseTree? Definition { get; }

    protected Symbol(string name)
    {
        this.Name = name;
    }

    public ApiSymbol ToApiSymbol() => new(this);
}

internal abstract partial class Symbol
{
    // Base class for "real" symbols found in the tree
    public abstract class InTreeBase : Symbol
    {
        public override ParseTree Definition { get; }
        public override Scope? EnclosingScope =>
            SymbolResolution.GetContainingScopeOrNull(this.db, this.Definition).GetAwaiter().GetResult();

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
