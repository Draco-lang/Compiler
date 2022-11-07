using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Query;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// The base of all symbols.
/// </summary>
internal abstract partial class Symbol : ISymbol
{
    public string Name { get; }

    // NOTE: Not a good idea
    public bool IsGlobal =>
        SymbolResolution.GetContainingScope(this.db, this.definition).Result?.Kind == ScopeKind.Global;

    private readonly QueryDatabase db;
    private readonly ParseTree definition;

    public Symbol(QueryDatabase db, ParseTree definition, string name)
    {
        this.db = db;
        this.definition = definition;
        this.Name = name;
    }
}

internal abstract partial class Symbol
{
    /// <summary>
    /// A symbol for a label.
    /// </summary>
    public sealed class Label : Symbol
    {
        public Label(QueryDatabase db, ParseTree definition, string name)
            : base(db, definition, name)
        {
        }
    }
}

internal abstract partial class Symbol
{
    /// <summary>
    /// A symbol for a function declaration.
    /// </summary>
    public sealed class Function : Symbol
    {
        public Function(QueryDatabase db, ParseTree definition, string name)
            : base(db, definition, name)
        {
        }
    }
}

internal abstract partial class Symbol
{
    /// <summary>
    /// A symbol for a variable declaration.
    /// </summary>
    public sealed class Variable : Symbol
    {
        public bool IsMutable { get; }

        public Variable(QueryDatabase db, ParseTree definition, string name, bool isMutable)
            : base(db, definition, name)
        {
            this.IsMutable = isMutable;
        }
    }
}
