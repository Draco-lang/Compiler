using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Fuzzer.Generators;

/// <summary>
/// Character set constants.
/// </summary>
internal static class Charsets
{
    /// <summary>
    /// The graphical (visible) subset of ASCII.
    /// </summary>
    public const string GraphicalAscii = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    /// <summary>
    /// The printable subset of ASCII.
    /// </summary>
    public const string PrintableAscii = $" {GraphicalAscii}";

    /// <summary>
    /// The printable subset of ASCII plus newlines and tabs.
    /// </summary>
    public const string PrintableAsciiWithWhitespaces = $"\r\n\t{PrintableAscii}";
}
