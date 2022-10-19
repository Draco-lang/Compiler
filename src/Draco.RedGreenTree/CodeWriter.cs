using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree;

internal sealed class CodeWriter
{
    public string Code => this.builder.ToString();

    private char LastWritten => this.builder.Length == 0 ? '\0' : this.builder[this.builder.Length - 1];
    private bool IsBlankLine => this.LastWritten is '\n' or '\r';

    private readonly StringBuilder builder = new();
    private int indentation;

    public CodeWriter OpenBrace()
    {
        this.BlankLine();
        this.WriteLine("{");
        ++this.indentation;
        return this;
    }

    public CodeWriter CloseBrace()
    {
        --this.indentation;
        this.BlankLine();
        this.WriteLine("}");
        return this;
    }

    public CodeWriter WriteDocs(string doc)
    {
        using var reader = new StringReader(doc);
        while (true)
        {
            var line = reader.ReadLine();
            if (line is null) break;
            this.Write("/// ").WriteLine(line);
        }
        return this;
    }

    public CodeWriter Write(Accessibility accessibility) =>
        this.Separate().Write(accessibility.ToString().ToLower());

    public CodeWriter Write(INamedTypeSymbol type) =>
        this.Separate().Write(type.ToDisplayString());

    public CodeWriter BlankLine()
    {
        if (!this.IsBlankLine) this.WriteLine();
        return this;
    }

    public CodeWriter Separate()
    {
        const string separatorChars = "\0\t\r\n (){}[].,:;";
        if (!separatorChars.Contains(this.LastWritten)) this.Write(' ');
        return this;
    }

    public CodeWriter Write(string text)
    {
        this.Indent();
        this.builder.Append(text);
        return this;
    }

    public CodeWriter Write(char ch)
    {
        this.Indent();
        this.builder.Append(ch);
        return this;
    }

    public CodeWriter WriteLine(string text)
    {
        this.Indent();
        this.builder.AppendLine(text);
        return this;
    }

    public CodeWriter WriteLine()
    {
        this.builder.AppendLine();
        return this;
    }

    private void Indent()
    {
        if (!this.IsBlankLine) return;
        for (var i = 0; i < this.indentation; ++i) this.builder.Append("    ");
    }
}
