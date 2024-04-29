using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal struct TokenDecoration
{
    private ScopeInfo scopeInfo;
    private string? tokenOverride;
    private Api.Syntax.SyntaxToken token;

    public FormattingTokenKind Kind { get; set; }

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
        readonly get => this.tokenOverride;
        set
        {
            if (this.tokenOverride != null) throw new InvalidOperationException("Override already set");
            this.tokenOverride = value;
        }
    }

    [DisallowNull]
    public Box<bool?>? DoesReturnLine { readonly get; set; }

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

    public IReadOnlyCollection<string> LeadingComments { get; set; }
}
