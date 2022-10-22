using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Draco.RedGreenTree;

internal sealed class CodeWriter
{
    public string Code => SyntaxFactory
        .ParseCompilationUnit(this.builder.ToString())
        .NormalizeWhitespace()
        .GetText()
        .ToString();

    private readonly StringBuilder builder = new();

    public override string ToString() => this.Code;

    public CodeWriter Write(Accessibility accessibility) =>
        this.Write(accessibility.ToString().ToLower());

    public CodeWriter Write(ISymbol symbol) =>
        this.Write(symbol.ToDisplayString());

    public CodeWriter Write(CodeWriter other) =>
        this.Write(other.ToString());

    public CodeWriter Write(string text)
    {
        this.builder.AppendLine(text);
        return this;
    }
}
