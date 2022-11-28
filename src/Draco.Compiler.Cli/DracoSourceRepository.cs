using Errata;

namespace Draco.Compiler.Cli;

internal sealed class DracoSourceRepository : ISourceRepository
{
    private readonly Source SourceCode;

    public DracoSourceRepository(string sourceText)
    {
        this.SourceCode = new Source("0", sourceText.Replace("\r\n", "\n"));
    }
    public bool TryGet(string id, out Source source)
    {
        source = this.SourceCode;
        return true;
    }
}
