using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Codegen;

internal readonly record struct SequencePoint(
    int IlOffset,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn,
    DocumentHandle Document)
{
    public bool IsHidden =>
           this.StartLine == 0xfeefee
        && this.EndLine == 0xfeefee
        && this.StartColumn == 0
        && this.EndColumn == 0;

    public static SequencePoint Hidden(int ilOffset, DocumentHandle document) => new(
        IlOffset: ilOffset,
        Document: document,
        StartLine: 0xfeefee,
        EndLine: 0xfeefee,
        StartColumn: 0,
        EndColumn: 0);
}
