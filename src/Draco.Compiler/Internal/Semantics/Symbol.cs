using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api.Semantics;
using Draco.Query;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// The base of all symbols.
/// </summary>
internal abstract partial class Symbol : ISymbol
{
    public string Name { get; }
    public abstract Scope? EnclosingScope { get; }
    public abstract ParseTree? DefinitionTree { get; }
    public virtual bool IsError => false;
    public virtual ImmutableArray<Diagnostic> Diagnostics => ImmutableArray<Diagnostic>.Empty;
    public Location? Definition => this.DefinitionTree?.Green.Location;

    private ImmutableArray<Api.Diagnostics.Diagnostic>? diagnostics;
    ImmutableArray<Api.Diagnostics.Diagnostic> ISymbol.Diagnostics => this.diagnostics ??= this.Diagnostics
        .Select(d => new Api.Diagnostics.Diagnostic(d, (this as ISymbol).Definition))
        .ToImmutableArray();
    Api.Diagnostics.Location? ISymbol.Definition => this.DefinitionTree?.Location;

    protected Symbol(string name)
    {
        this.Name = name;
    }
}

internal abstract partial class Symbol
{
    // Base class for "real" symbols found in the tree
    public abstract class InTreeBase : Symbol
    {
        public override ParseTree DefinitionTree { get; }
        // NOTE: Not a good idea to rely on .Result
        public override Scope? EnclosingScope =>
            SymbolResolution.GetContainingScopeOrNull(this.db, this.DefinitionTree).Result;

        private readonly QueryDatabase db;

        protected InTreeBase(QueryDatabase db, string name, ParseTree definition)
            : base(name)
        {
            this.db = db;
            this.DefinitionTree = definition;
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
        public override ParseTree? DefinitionTree { get; }
        // TODO: Should be what is below
        // public override ParseTree? DefinitionTree => null;
        public override Scope? EnclosingScope => null;
        public override bool IsError => true;
        public override ImmutableArray<Diagnostic> Diagnostics { get; }

        // TODO: NO, THE ERROR SHOULD NOT TAKE THE DEFINITION AS A TREE
        // BUT THE LOCATION API NEEDS TO BE CLEANED UP
        public Error(string name, ImmutableArray<Diagnostic> diagnostics, ParseTree definitionTree)
            : base(name)
        {
            this.Diagnostics = diagnostics;
            this.DefinitionTree = definitionTree;
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
        public override ParseTree? DefinitionTree => null;

        public Intrinsic(string name)
            : base(name)
        {
        }
    }
}
