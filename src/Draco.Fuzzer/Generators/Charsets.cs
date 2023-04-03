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
    /// The letter characters from ASCII.
    /// </summary>
    public const string AsciiLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// The digits from ASCII.
    /// </summary>
    public const string AsciiDigits = "0123456789";

    /// <summary>
    /// The letters and digits from ASCII.
    /// </summary>
    public const string AsciiLettersAndDigits = $"{AsciiLetters}{AsciiDigits}";

    /// <summary>
    /// The graphical (visible) subset of ASCII.
    /// </summary>
    public const string GraphicalAscii = $"!\"#$%&'()*+,-./:;<=>?@[\\]^_`{{|}}~{AsciiLettersAndDigits}";

    /// <summary>
    /// The printable subset of ASCII.
    /// </summary>
    public const string PrintableAscii = $" {GraphicalAscii}";

    /// <summary>
    /// The printable subset of ASCII plus newlines and tabs.
    /// </summary>
    public const string PrintableAsciiWithWhitespaces = $"\r\n\t{PrintableAscii}";
}
