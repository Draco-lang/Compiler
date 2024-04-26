using System;
using System.Diagnostics.CodeAnalysis;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal struct TokenDecoration
{
    private ScopeInfo scopeInfo;
    private string? tokenOverride;
    private CollapsibleBool? doesReturnLineCollapsible;
    private FormattingTokenKind kind;
    private Api.Syntax.SyntaxToken token;
    private bool _kindIsSet;
    public FormattingTokenKind Kind
    {
        readonly get => this.kind;
        set
        {
            if (this._kindIsSet) throw new InvalidOperationException("Kind already set");
            this.kind = value;
            this._kindIsSet = true;
        }
    }
    public Api.Syntax.SyntaxToken Token
    {
        readonly get => this.token; set
        {
            if (this.token != null) throw new InvalidOperationException("Token already set");
            this.token = value;
        }
    }

    [DisallowNull]
    public string? TokenOverride
    {
        get => this.tokenOverride;
        set
        {
            if (this.tokenOverride != null) throw new InvalidOperationException("Override already set");
            this.tokenOverride = value;
        }
    }

    [DisallowNull]
    public CollapsibleBool? DoesReturnLineCollapsible
    {
        readonly get => this.doesReturnLineCollapsible;
        set
        {
            if (this.doesReturnLineCollapsible != null) throw new InvalidOperationException("Collapsible already set");
            this.doesReturnLineCollapsible = value;
        }
    }

    public ScopeInfo ScopeInfo
    {
        readonly get => this.scopeInfo;
        set
        {
            if (this.scopeInfo != null)
            {
                throw new InvalidOperationException();
            }
            this.scopeInfo = value;
        }
    }
}
