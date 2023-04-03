using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Codegen;

internal readonly record struct SequencePoint(
    DocumentHandle Document,
    int IlOffset,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn)
{
    public static SequencePoint Hidden(int ilOffset) => Hidden(default, ilOffset);

    public static SequencePoint Hidden(DocumentHandle document, int ilOffset) => new(
        Document: document,
        IlOffset: ilOffset,
        StartLine: 0xfeefee,
        EndLine: 0xfeefee,
        StartColumn: 0,
        EndColumn: 0);

    public SequencePoint(int ilOffset, int startLine, int startColumn, int endLine, int endColumn)
        : this(default, ilOffset, startLine, startColumn, endLine, endColumn)
    {
    }

    public bool IsHidden =>
           this.StartLine == 0xfeefee
        && this.EndLine == 0xfeefee
        && this.StartColumn == 0
        && this.EndColumn == 0;
}
