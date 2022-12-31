using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// An in-memory <see cref="Api.Syntax.SourceText"/> implementation.
/// </summary>
internal sealed class MemorySourceText : Api.Syntax.SourceText
{
    public override Uri? Path { get; }
    internal override ISourceReader SourceReader => Syntax.SourceReader.From(this.content);

    private readonly ReadOnlyMemory<char> content;

    public MemorySourceText(Uri? path, ReadOnlyMemory<char> content)
    {
        this.Path = path;
        this.content = content;
    }
}
