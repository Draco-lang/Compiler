using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.FlowAnalysis.Domain;

internal sealed class StringDomain : ValueDomain
{
    public override bool IsEmpty => this.allSubtracted;

    private readonly TypeSymbol backingType;
    private readonly HashSet<string> subtracted;
    private bool allSubtracted;

    private StringDomain(TypeSymbol backingType, HashSet<string> subtracted, bool allSubtracted)
    {
        this.backingType = backingType;
        this.subtracted = subtracted;
        this.allSubtracted = allSubtracted;
    }

    public StringDomain(TypeSymbol backingType)
        : this(backingType, new(), false)
    {
    }

    public override string ToString() => this.allSubtracted
        ? "empty"
        : $"Universe \\ {{{string.Join(", ", this.subtracted.Select(StringUtils.Unescape))}}}";

    public override ValueDomain Clone() =>
        new StringDomain(this.backingType, this.subtracted.ToHashSet(), this.allSubtracted);

    public override BoundPattern? SamplePattern()
    {
        if (this.allSubtracted) return null;

        // Check some trivial ones
        if (!this.subtracted.Contains(string.Empty)) return this.ToPattern(string.Empty);

        // We use the same trick as proving that you can't list all possible reals
        // We construct a string that differs in the first char from the first string,
        // in the second char of the second string, ...

        var result = new StringBuilder();
        var i = 0;
        foreach (var str in this.subtracted)
        {
            var ithChar = str.Length < i ? str[i] : '\0';
            result.Append(ithChar == 'a' ? 'b' : 'a');
        }

        return this.ToPattern(result.ToString());
    }

    public override void SubtractPattern(BoundPattern pattern)
    {
        if (this.allSubtracted) return;

        switch (pattern)
        {
        case BoundDiscardPattern:
            this.subtracted.Clear();
            this.allSubtracted = true;
            break;
        case BoundLiteralPattern litPattern when litPattern.Value is string str:
            this.subtracted.Add(str);
            break;
        default:
            throw new ArgumentException("illegal pattern for string domain", nameof(pattern));
        }
    }

    private BoundPattern ToPattern(string text) => throw new NotImplementedException();
}
