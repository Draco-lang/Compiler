using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

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
    /// Creates a metadata reference from the given assembly.
    /// </summary>
    /// <param name="assembly">The assembly to create a metadata reader from.</param>
    /// <returns>The <see cref="MetadataReference"/> created from <paramref name="assembly"/>.</returns>
    public static MetadataReference FromAssembly(Assembly assembly)
    {
        unsafe
        {
            if (!assembly.TryGetRawMetadata(out var blob, out var length))
            {
                throw new ArgumentException("could not retrieve metadata section from assembly", nameof(assembly));
            }

            var reader = new MetadataReader(blob, length);
            return new MetadataReaderReference(reader);
        }
    }

    private sealed class MetadataReaderReference : MetadataReference
    {
        public override MetadataReader MetadataReader { get; }

        public MetadataReaderReference(MetadataReader metadataReader)
        {
            this.MetadataReader = metadataReader;
        }
    }

    public static MetadataReference FromPEStream(Stream peStream)
    {
        return new PEStreamReference(peStream);
    }

    private sealed class PEStreamReference : MetadataReference
    {
        public override MetadataReader MetadataReader => this.metadataReader ??= this.peReader.GetMetadataReader();

        private readonly PEReader peReader;
        private MetadataReader? metadataReader;

        public PEStreamReference(Stream peStream)
        {
            this.peReader = new PEReader(peStream);
        }
    }
}
