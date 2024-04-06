using System;
using System.Diagnostics.CodeAnalysis;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal struct TokenDecoration
{
    private string? rightPadding;
    private string? leftPadding;
    private SolverTask<string?>? indentation;
    private ScopeInfo scopeInfo;
    private string? tokenOverride;

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
    public readonly int CurrentTotalSize => this.TokenSize + (this.leftPadding?.Length ?? 0) + (this.rightPadding?.Length ?? 0) + this.CurrentIndentationSize;

    private readonly int CurrentIndentationSize => this.Indentation?.IsCompleted ?? false ? this.Indentation.Result?.Length ?? 0 : 0;

    [DisallowNull]
    public CollapsibleBool? DoesReturnLineCollapsible { get; private set; }

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
    public readonly SolverTask<string?>? Indentation => this.indentation;

    public void SetIndentation(SolverTask<string?>? value)
    {
        if (this.indentation is not null)
        {
            if (this.indentation.IsCompleted && value.IsCompleted && this.indentation.Result == value.Result) return;
            throw new InvalidOperationException("Indentation already set.");
        }
        var doesReturnLine = this.DoesReturnLineCollapsible = CollapsibleBool.Create();
        this.indentation = value;
        this.indentation!.Awaiter.OnCompleted(() =>
        {
            doesReturnLine.Collapse(value.Result != null);
        });
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
