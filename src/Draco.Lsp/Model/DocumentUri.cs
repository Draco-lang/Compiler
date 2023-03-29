using System;
using Draco.Lsp.Serialization;
using Newtonsoft.Json;

namespace Draco.Lsp.Model;

[JsonConverter(typeof(DocumentUriConverter))]
public readonly record struct DocumentUri : IEquatable<DocumentUri>
{
    public static DocumentUri From(Uri uri) => new(uri);

    private readonly Uri uri;

    private DocumentUri(Uri uri)
    {
        this.uri = uri;
    }

    public DocumentUri(string path)
        : this(new Uri(path))
    {
    }

    public Uri ToUri() => this.uri;

    public override string ToString() => this.uri.ToString();
}
