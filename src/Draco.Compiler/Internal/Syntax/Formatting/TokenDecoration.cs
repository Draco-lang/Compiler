using System;
using System.Diagnostics.CodeAnalysis;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal struct TokenDecoration
{
    private string? rightPadding;
    private string? leftPadding;
    private ScopeInfo scopeInfo;
    private string? tokenOverride;
    private CollapsibleBool? doesReturnLineCollapsible;

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
    public int TokenSize { get; set; }

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

    public string? LeftPadding
    {
        readonly get => this.leftPadding;
        set
        {
            if (this.leftPadding is not null) throw new InvalidOperationException("Left padding already set.");
            this.leftPadding = value;
        }
    }
    public string? RightPadding
    {
        readonly get => this.rightPadding;
        set
        {
            if (this.rightPadding is not null) throw new InvalidOperationException("Right padding already set.");
            this.rightPadding = value;
        }
    }

}
