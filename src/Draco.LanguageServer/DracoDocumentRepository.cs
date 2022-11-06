using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using static Draco.Compiler.Api.Syntax.ParseTree.Expr;

namespace Draco.LanguageServer;

internal static class DracoDocumentRepository
{
    public static DracoDocumentList Documents { get; } = new DracoDocumentList();
}

internal class DracoDocumentList
{
    public DracoDocument this[string index]
    {
        get
        {
            var doc = this.documents.Where(d => d.Path == index).ToList();
            if (doc.Count == 0)
            {
                throw new Exception($"Document with the path {index} was not registered");
            }
            else if (doc.Count == 1)
            {
                return doc[0];
            }
            else
            {
                throw new Exception($"There were multiple documents with the path {index} registered");
            }
        }
    }

    public void AddOrUpdateDocument(string path, string contents)
    {
        var doc = this.documents.Where(d => d.Path == path).ToList();
        if (doc.Count == 0)
        {
            this.documents.Add(new DracoDocument(contents, path));
        }
        else if (doc.Count == 1)
        {
            doc[0].Contents = contents;
        }
        else
        {
            throw new Exception($"There were multiple documents with the path {path} registered");
        }
    }
    private List<DracoDocument> documents = new List<DracoDocument>();
}

internal class DracoDocument
{
    public string Contents { get; set; }
    public string Path { get; private set; }
    public DracoDocument(string contents, string path)
    {
        this.Contents = contents;
        this.Path = path;
    }
}
