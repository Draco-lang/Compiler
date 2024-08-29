using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;

namespace Draco.Compiler.Api;

/// <summary>
/// Represents some kind of metadata reference.
/// </summary>
public abstract class MetadataReference
{
    /// <summary>
    /// Retrieves the metadata reader for this reference.
    /// </summary>
    public abstract MetadataReader MetadataReader { get; }

    /// <summary>
    /// The documentation for this reference.
    /// </summary>
    public abstract XmlDocument? Documentation { get; }

    /// <summary>
    /// Creates a metadata reference reading up the given file.
    /// </summary>
    /// <param name="path">The path to the file to read.</param>
    /// <returns>The <see cref="MetadataReference"/> created from the file at <paramref name="path"/>.</returns>
    public static MetadataReference FromFile(string path)
    {
        // NOTE: We could add a bool to turn off fetching the docs
        var peStream = File.OpenRead(path);
        // We assume the docs are under the same path, same name but with .xml extension
        var docPath = Path.ChangeExtension(path, ".xml");
        var docStream = File.Exists(docPath) ? File.OpenRead(docPath) : null;
        var docXml = null as XmlDocument;
        if (docStream is not null)
        {
            docXml = new XmlDocument();
            docXml.Load(docStream);
        }
        return FromPeStream(peStream, docXml);
    }

    /// <summary>
    /// Creates a metadata reference from the given assembly.
    /// </summary>
    /// <param name="assembly">The assembly to create a metadata reader from.</param>
    /// <param name="documentation">The XML documentation for the assembly.</param>
    /// <returns>The <see cref="MetadataReference"/> created from <paramref name="assembly"/>.</returns>
    public static MetadataReference FromAssembly(Assembly assembly, XmlDocument? documentation = null)
    {
        unsafe
        {
            if (!assembly.TryGetRawMetadata(out var blob, out var length))
            {
                throw new ArgumentException("could not retrieve metadata section from assembly", nameof(assembly));
            }

            var reader = new MetadataReader(blob, length);
            return new MetadataReaderReference(reader, documentation);
        }
    }

    /// <summary>
    /// Creates a metadata reference from the given PE stream.
    /// </summary>
    /// <param name="peStream">The PE stream to create the metadata reference from.</param>
    /// <param name="documentation">The XML documentation for the assembly.</param>
    /// <returns>The <see cref="MetadataReference"/> reading up from <paramref name="peStream"/>.</returns>
    public static MetadataReference FromPeStream(Stream peStream, XmlDocument? documentation = null)
    {
        var peReader = new PEReader(peStream);
        var metadataReader = peReader.GetMetadataReader();
        return new MetadataReaderReference(metadataReader, documentation);
    }

    private sealed class MetadataReaderReference(
        MetadataReader metadataReader,
        XmlDocument? documentation) : MetadataReference
    {
        public override MetadataReader MetadataReader { get; } = metadataReader;
        public override XmlDocument? Documentation { get; } = documentation;
    }
}
