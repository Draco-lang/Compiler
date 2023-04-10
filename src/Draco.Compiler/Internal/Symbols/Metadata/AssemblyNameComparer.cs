using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Utility for comparing assembly names.
/// </summary>
internal static class AssemblyNameComparer
{
    /// <summary>
    /// Compares the full names of assemblies.
    /// </summary>
    public static IEqualityComparer<AssemblyName> Full { get; } = new FullAssemblyNameComparer();

    /// <summary>
    /// Compares the name and public key token of assemblies.
    /// </summary>
    public static IEqualityComparer<AssemblyName> NameAndToken { get; } = new NameTokenAssemblyNameComparer();

    private sealed class FullAssemblyNameComparer : IEqualityComparer<AssemblyName>
    {
        public bool Equals(AssemblyName? x, AssemblyName? y) => x?.FullName == y?.FullName;
        public int GetHashCode([DisallowNull] AssemblyName obj) => obj.FullName.GetHashCode();
    }

    private sealed class NameTokenAssemblyNameComparer : IEqualityComparer<AssemblyName>
    {
        public bool Equals(AssemblyName? x, AssemblyName? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            var xToken = x.GetPublicKeyToken()?.AsMemory() ?? ReadOnlyMemory<byte>.Empty;
            var yToken = y.GetPublicKeyToken()?.AsMemory() ?? ReadOnlyMemory<byte>.Empty;

            return x.Name == y.Name
                && xToken.Span.SequenceEqual(yToken.Span);
        }

        public int GetHashCode([DisallowNull] AssemblyName obj)
        {
            var h = default(HashCode);
            h.Add(obj.Name);
            var token = obj.GetPublicKeyToken();
            if (token is not null)
            {
                foreach (var b in token) h.Add(b);
            }
            return h.ToHashCode();
        }
    }
}
